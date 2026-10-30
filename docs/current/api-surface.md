# Current API Surface

Status: Current

This document lists all API endpoints that currently exist in the codebase, their status, and known gaps.

---

## Authorization API

Base URL (local): `http://localhost:8080`
Base URL (production): `https://timeforcode-auth-api.azurewebsites.net`

The Authorization API exposes OAuth 2.0 endpoints and an OpenID Connect discovery document so downstream services can validate tokens.

### Authentication Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `GET` | `/api/v1/authentication/login` | ✅ | Initiates OAuth 2.0 login; redirects to GitHub |
| `GET` | `/api/v1/authentication/callback` | ✅ | Handles OAuth 2.0 callback; exchanges code for tokens |
| `POST` | `/api/v1/authentication/refresh` | ✅ | Issues a new access token using the refresh token (POST only; GET returns 405) |
| `POST` | `/api/v1/authentication/logout` | ✅ | Clears access and refresh token cookies |

### Test-only admin endpoint

Not part of the published contract: hidden from the OpenAPI document and absent from the Swagger snapshot. It exists so live tests can act as the admin without a WebAuthn passkey, and is off unless `TestAdminAccess:Enabled` is `true` (see [ADR-017](../reference/adr/0017-test-only-admin-access.md)).

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/test-admin-access/token` | ✅ | Body `{ "secret": "..." }`. `200` with `{ "token", "expiresAfter" }`: an admin access token (scope and role `admin`, claim `amr=test`, at most 15 minutes, no refresh token, no cookies). `401` for a wrong secret, `404` when the endpoint is disabled (the default) |

### User Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `GET` | `/api/v1/user` | ✅ | Returns the currently authenticated user's profile |
| `DELETE` | `/api/v1/user` | ✅ | Deletes the caller's account: asks the Donation API to anonymise the contributor, then deletes the refresh tokens and the profile and expires both token cookies; `204` on success, `404` when the profile does not exist, `502` when the Donation API fails (nothing is removed, retry later), `401` without a token. See [personal data](../reference/personal-data.md) |
| `GET` | `/api/v1/user/repositories` | ✅ | Returns the caller's public GitHub repositories, read with the application's own GitHub credentials (no user token is stored); `502` when GitHub fails |
| `GET` | `/api/v1/user/repositories/{owner}/{repo}` | ✅ | Returns one public GitHub repository (`404` for private or unknown ones), read with the application's own GitHub credentials; `400` when `owner` or `repo` is not a valid GitHub name (see [declared input limits](#declared-input-limits)) |

### OpenID Connect / JWKS

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `GET` | `/.well-known/openid-configuration` | ✅ | OpenID Connect discovery document |
| `GET` | `/.well-known/jwks.json` | ✅ | Public key set for JWT validation: the current and, while configured, the previous signing key, each with `kid`, `use` (`sig`) and `alg` (`RS256`) |

---

## Donation API

Base URL (local): `http://localhost:8082`

The Donation API is responsible for projects, donations, organizations, and contributors.

### Project Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/project` | ✅ | Registers a public GitHub repository as a project in state `Draft` (user JWT required); fetches full metadata from GitHub; returns 400 for private/archived repos, 409 if the repository is already registered in any state |
| `GET` | `/api/v1/project` | ✅ | Searches `Active` projects with optional text, language and tag filters and paging; no authentication required (see [Project search](#project-search)) |
| `GET` | `/api/v1/project/{id}` | ✅ | Returns full project details of an `Active` project (404 otherwise); no authentication required |
| `GET` | `/api/v1/project/{id}/manage` | ✅ | Maintainer reads their own project in any state (user JWT, owner only); returns `{ "project": { ..., "status" }, "reviewerReason" }`; `403` for another user's project, `404` unknown project |
| `POST` | `/api/v1/project/{id}/submit` | ✅ | Maintainer submits a `Draft` project for review → `PendingApproval` (user JWT, owner only) |
| `POST` | `/api/v1/project/{id}/approve` | ✅ | Administrator approves a `PendingApproval` project → `Active` (admin JWT, scope `admin`) |
| `POST` | `/api/v1/project/{id}/request-changes` | ✅ | Administrator returns a `PendingApproval` project to `Draft`; body `{ "reason": "..." }` (max 1000 chars) is stored and returned as `reviewerReason` (admin JWT) |
| `POST` | `/api/v1/project/{id}/archive` | ✅ | Maintainer archives an `Active` project → `Archived` (user JWT, owner only); replaces the former `DELETE /api/v1/project/{id}` |
| `POST` | `/api/v1/project/{id}/reactivate` | ✅ | Maintainer re-activates an `Archived` project → `Active` (user JWT, owner only) |

The lifecycle endpoints return `200 OK` with `{ "projectId", "status", "reviewerReason" }`. Error cases: `401` no/invalid token, `403` wrong role or not the project's maintainer, `404` unknown project, `409` transition not allowed from the current state (the problem details name the required state), `400` invalid request body.

Example request-changes call and response:

```http
POST /api/v1/project/5f43a0e74b12c84f1b000001/request-changes
Authorization: Bearer <admin token>
Content-Type: application/json

{ "reason": "Please add a description" }
```

```json
{ "projectId": "5f43a0e74b12c84f1b000001", "status": "Draft", "reviewerReason": "Please add a description" }
```

Example `409` problem details: `{ "title": "Transition not allowed", "status": 409, "detail": "A project can only be approved from state PendingApproval, but it is Draft." }`.

#### Project search

`GET /api/v1/project` is anonymous and only ever returns `Active` projects; `Draft`, `PendingApproval` and `Archived` projects are never exposed.

| Query parameter | Default | Description |
| --- | --- | --- |
| `searchText` | none | Case-insensitive literal substring match on the project name, full name or description (regex characters are escaped); at most 100 characters |
| `language` | none | Exact, case-sensitive match on the GitHub language, for example `C#`; at most 100 characters |
| `tag` | none | Case-insensitive match on one GitHub topic (topics are stored lowercase); at most 100 characters |
| `pageNumber` | `1` | 1-based page number, 1 to 10000 |
| `pageSize` | `20` | Items per page, 1 to 100 |

Supplied criteria are combined with AND; leading and trailing whitespace is trimmed and blank values count as absent. Sort order is fixed: publication date (`publishedAt`) newest first, then project id descending, so pages are stable.

```http
GET /api/v1/project?searchText=rocket&language=C%23&tag=gaming&pageNumber=1&pageSize=20
```

```json
{
  "projects": [
    {
      "id": "5f43a0e74b12c84f1b000001",
      "name": "rocket-planner",
      "fullName": "owner/rocket-planner",
      "description": "Plan your launches",
      "githubUrl": "https://github.com/owner/rocket-planner",
      "language": "C#",
      "stargazersCount": 42,
      "forksCount": 5,
      "openIssuesCount": 3,
      "ownerLogin": "owner",
      "ownerAvatarUrl": "https://avatars.githubusercontent.com/u/1?v=4"
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20
}
```

`totalCount` is the number of projects matching all criteria, not the page length; no matches returns `200` with an empty `projects` array and `totalCount` `0`. The limits are also declared in the OpenAPI document (`minimum`, `maximum`, `maxLength`) so generated clients can see them. Error cases: `400` validation problem details (`errors` lists the offending parameter) for `pageNumber` outside 1 to 10000, `pageSize` outside 1 to 100, or a `searchText`, `language` or `tag` longer than 100 characters; `500` unexpected error, including a search that exceeds the 5 second server-side time limit.

### Donation Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/project/{id}/donation` | ✅ | Pledges hours to an `Active` project (user JWT); body `{ "hours": 1-10000, "asIndividual": true }` or `{ "hours": 20, "donorOrganizationId": "<id>" }`, exactly one donor; returns `201` with the donation (id, project id, donor organization id when given, `status` `Pending`, `hoursCommitted`, `hoursLogged` 0, `progressPercentage`, `createdAt`, `updatedAt`); `400` for invalid hours, no donor or both donors, `401` without a token, `404` for an unknown project or donor organization, `409` when the project is not `Active` |
| `GET` | `/api/v1/project/{id}/donation` | ✅ | Lists the donations of an `Active` project, oldest first, as `{ "donations": [...] }` (at most 1000); no authentication required; `404` for an unknown or non-`Active` project |
| `GET` | `/api/v1/donation` | ✅ | Lists the caller's own donations, oldest first, as `{ "donations": [...] }` (user JWT); `401` without a token |
| `GET` | `/api/v1/donororganization/{id}/donation` | ✅ | Lists the donations pledged for a donor organization, oldest first, as `{ "donations": [...] }` (user or admin JWT; contributors affiliated with the organization and administrators only); `400` for a malformed id or missing identity, `401` without a token, `403` for other users, `404` for an unknown organization |
| `POST` | `/api/v1/donation/{id}/activate` | ✅ | Pending -> Active; allowed for the donor who pledged it and for administrators (scope `user` or `admin`); returns `200` with the donation; `403` for other users, `404` unknown donation, `409` when not `Pending` |
| `POST` | `/api/v1/donation/{id}/cancel` | ✅ | Pending or Active -> Cancelled; same permissions as activate; `409` when `Completed` or `Cancelled` |

Activate and cancel answer `409` when the donation was changed concurrently (optimistic `version` check); reload and retry. Data reset (ADR-014): donations are stored in their own `Donation` collection with a `version` field, and `DonorOrganization.Donations` no longer exists; drop old `Donation` documents and organization `Donations` arrays before deploying.

A donation completes automatically (Active -> Completed) when the hours logged through `POST /api/v1/project/{id}/contribution` reach `hoursCommitted`. Logged hours count toward the project's `Active` donations only, oldest pledge first, and spill over to the next donation. Donor organization responses carry `hoursSpent` (hours logged on all its donations) and `hoursAvailable` (committed minus logged on its `Active` donations).

### Organization Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/donororganization` | ✅ | Registers a donor organization; returns 400 for invalid input or 409 for a duplicate name |
| `GET` | `/api/v1/donororganization` | ✅ | Returns a paginated list of donor organizations; returns 400 for invalid pagination parameters |
| `GET` | `/api/v1/donororganization/{id}` | ✅ | Returns a donor organization's details; returns 404 when not found |
| `PUT` | `/api/v1/donororganization/{id}` | ✅ | Updates a donor organization; returns 404 when not found or 409 for a duplicate name |
| `DELETE` | `/api/v1/donororganization/{id}` | ✅ | Deletes a donor organization; returns 204 No Content on success or 404 when not found |

### Contribution Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/project/{id}/contribution` | ✅ | Logs hours on an `Active` project (user JWT); body `{ "hours": 1-10000, "githubReference": "https://github.com/..." }` (`githubReference` optional, HTTPS github.com only); upserts the caller as a `Contributor` by `sub` and returns `201` with the contribution (id, contributor id, GitHub login and avatar snapshot, hours, reference, `loggedAt`); `400` for invalid hours or reference or a token without `login`/`avatar_url` claims, `401` without a token, `404` for an unknown or non-`Active` project |
| `GET` | `/api/v1/donororganization/{id}/contribution` | ✅ | Lists the contributions logged by the contributors currently affiliated with a donor organization, earliest first, as `{ "contributions": [...] }` (at most 10000; deleted contributions are left out; user or admin JWT; contributors affiliated with the organization and administrators only); `400` for a malformed id or missing identity, `401` without a token, `403` for other users, `404` for an unknown organization |
| `PUT` | `/api/v1/project/{id}/contribution/{contributionId}` | ✅ | Corrects a contribution (user or admin JWT; the contributor who logged it or an administrator, scope `admin`); body `{ "hours": 1-10000, "githubReference": "https://github.com/..." }` replaces both values (an omitted `githubReference` removes it); the previous values are appended to the stored history with the corrector and time (the history is not exposed by the API); lowering the hours releases the difference from the donations the contribution was counted toward (newest allocation first; a `Completed` donation that falls below its commitment becomes `Active` again), raising them counts the extra hours toward the project's `Active` donations like logging does; `200` with the contribution; `400` for invalid hours or reference, `401` without a token, `403` for another user, `404` for an unknown or already deleted contribution, `409` when it was changed concurrently |
| `DELETE` | `/api/v1/project/{id}/contribution/{contributionId}` | ✅ | Soft deletes a contribution (user or admin JWT; the contributor who logged it or an administrator): it stays stored with who deleted it and when, no longer appears in the list and no longer counts towards the hours, and all its hours are released from the donations it was counted toward (a `Completed` donation that falls below its commitment becomes `Active` again); `204`; `401` without a token, `403` for another user, `404` for an unknown or already deleted contribution, `409` when it was changed concurrently |
| `GET` | `/api/v1/project/{id}/contribution` | ✅ | Lists the non-deleted contributions of an `Active` project, earliest first, as `{ "contributions": [...] }` (at most 1000); no authentication required; `404` for an unknown or non-`Active` project |

The user JWT issued by the Authorization API carries the `login` and `avatar_url` claims (GitHub login and avatar URL of the account) next to `sub`, `scope` and `aud`; the Donation API reads them to fill the contributor snapshot.

### Maintainer Endpoints

The maintainer profile is created (or its GitHub login and avatar refreshed) when a user registers a project with `POST /api/v1/project`; the token must carry the `login` and `avatar_url` claims, otherwise registration answers `400`. Owner-only project operations (`manage`, `submit`, `archive`, `reactivate`) compare the project's publisher with the caller and answer `403` for anyone else.

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `GET` | `/api/v1/maintainer/me` | ✅ | Returns the caller's maintainer profile (user JWT): `{ "id", "githubLogin", "avatarUrl" }`; `404` when the caller never registered a project, `401` without a token |
| `GET` | `/api/v1/maintainer/me/projects` | ✅ | Lists the caller's projects in any lifecycle state, oldest first (user JWT): `{ "projects": [ { "project": { ..., "status" }, "hoursDonated": 15 } ] }`; `hoursDonated` adds up the hours committed by `Active` and `Completed` donations (pending and cancelled ones are not counted); an empty list when the caller has no projects, `401` without a token |

### Contributor Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/contributor/{userId}/anonymise` | ✅ | Service call from the Authorization API at account deletion (token scope `service` only; `403` for user and admin tokens): replaces the contributor and the login and avatar snapshot on their contributions with a generic deleted user and keeps the hours; `204`, also when the user has no contributions or was already anonymised; `400` for a malformed id |
| `GET` | `/api/v1/contributor/me` | ✅ | Returns the caller's profile (user JWT): `{ id, githubLogin, avatarUrl, role, skills, hoursAvailable, donorOrganizationId }` (`role`, `hoursAvailable` and `donorOrganizationId` absent when not set, `skills` empty when none); `404` when the caller never became a contributor, `401` without a token |
| `PUT` | `/api/v1/contributor/me` | ✅ | Replaces the caller's profile and registers the caller as a contributor when needed, reusing the contributor that logging hours created (user JWT); body `{ "role": "Backend developer", "skills": ["C#", "MongoDB"], "hoursAvailable": 10, "donorOrganizationId": "<id>" }`, every field optional and left-out fields are cleared; skills are trimmed, blank ones dropped and duplicates merged ignoring case (first spelling wins); returns `200` with the profile; `400` for a role over 100 characters, more than 50 skills, a skill over 100 characters, `hoursAvailable` outside 0-10000 or a malformed organization id, `404` for an unknown donor organization, `401` without a token |
| `GET` | `/api/v1/contributor/{id}` | ✅ | Returns a profile by contributor id; only the contributor themselves (user JWT) and administrators; `200` with the profile, `403` for another user's profile, `404` for an unknown contributor, `400` for a malformed id, `401` without a token |
| `PUT` | `/api/v1/contributor/{id}` | ✅ | Replaces a profile by contributor id, same body and responses as `PUT /contributor/me` plus `403` for another user's profile; allowed for the contributor themselves and administrators |
| `GET` | `/api/v1/donororganization/{id}/contributor` | ✅ | Lists the contributors affiliated with a donor organization, ordered by GitHub login (at most 1000; admin JWT only; `403` for user tokens); `200` with `{ "contributors": [ ...profiles ] }`, empty when there are none; `404` for an unknown organization, `400` for a malformed id, `401` without a token |

---

## Reporting API

The Reporting API (`:8083` locally) is back-end only. It reads the donations and contributions of a donor organization from the Donation API over HTTP with the caller's own bearer token, so the Donation API decides who may read: contributors affiliated with the organization and administrators (see DEBT-19: affiliation is self-declared). Reporting has no database and never writes to the Donation API. The caller needs a token whose audiences include the Reporting API (user or admin JWT).

### Reporting Endpoints

| Method | Endpoint | Status | Notes |
| --- | --- | --- | --- |
| `GET` | `/api/v1/reporting/donororganization/{id}/impact-report` | ✅ | Impact report of a donor organization (user or admin JWT); `200` with `{ "hoursPledged": 10, "hoursLogged": 4, "projectsSupported": 1, "projects": [ { "projectId": "...", "hoursPledged": 10, "hoursLogged": 4 } ] }`; only `Active` and `Completed` donations count, projects are listed in the order of their first donation, an organization without counted donations gets zeros and an empty list; `400` for a malformed id or a missing bearer token, `401` without a token, `403` when the caller is neither a member nor an administrator, `404` for an unknown organization, `502` when the Donation API fails or cannot be reached |
| `GET` | `/api/v1/reporting/donororganization/{id}/contribution-history?format=Json\|Csv` | ✅ | Export of the contribution history as a file download (user or admin JWT); `format` is `Json` (default, `application/json`) or `Csv` (`text/csv`); same status codes as the impact report; at most 10000 contributions, earliest first, deleted ones left out, attributed by the contributor's current affiliation (DEBT-21) |

Export schema (the same values in both formats):

| CSV column | JSON field | Type | Notes |
| --- | --- | --- | --- |
| `ContributionId` | `id` | string | Contribution identifier |
| `ProjectId` | `projectId` | string | Project the hours were logged on |
| `ContributorLogin` | `contributorLogin` | string | GitHub login of the contributor |
| `Hours` | `hours` | integer | Hours logged |
| `GithubReference` | `githubReference` | string or `null` | Issue or pull request URL; empty in CSV, `null` in JSON when none was given |
| `LoggedAt` | `loggedAt` | string | UTC, ISO 8601 (`2026-10-02T07:30:15Z`) |

The CSV has a header row and `\r\n` line endings. Values with a comma, quote or line break are quoted (RFC 4180), and a value starting with `=`, `+`, `-`, `@`, a tab or a carriage return gets a leading `'` so a spreadsheet does not run it as a formula. The JSON is `{ "contributions": [ ... ] }`, an empty list when there are none.

---

## API Standards

All implemented endpoints follow these conventions:

- Authentication: Bearer token (internal JWT from Authorization API) passed via cookie or `Authorization` header.
- Content type: `application/json`.
- API versioning: URL path prefix (`/api/v1/`).
- Error format: standard ASP.NET Core `ProblemDetails`. Input that violates a declared limit returns `400` with validation problem details (title `One or more validation errors occurred.`, an `errors` entry per parameter).
- Input limits are declared in the OpenAPI document (`minimum`, `maximum`, `minLength`, `maxLength`, `pattern`, `format`) and enforced; generated clients can read them. Property examples come from the XML documentation.
- Rate limiting: rate-limited operations document a `429 Too Many Requests` response. The response carries a `Retry-After` header with the number of seconds to wait.
- API documentation: Swagger UI available at `/swagger` in Development mode.

### Declared input limits

| Where | Limit |
| --- | --- |
| Donation: `{id}` of project and donor organization | Exactly 24 hexadecimal characters (`^[0-9a-fA-F]{24}$`); anything else is `400`, not `404` |
| Donation: `pageNumber` / `pageSize` on both list endpoints | 1 to 10000 / 1 to 100 |
| Donation: donor organization `name` | 1 to 200 characters, with at least one non-whitespace character |
| Donation: donor organization `contactEmail` | Email address (`format: email`), at most 254 characters; omit it instead of sending an empty string |
| Donation: donor organization `website` | Absolute http, https or ftp URL (`format: uri`), at most 2048 characters; omit it instead of sending an empty string |
| Donation: request-changes `reason` | 1 to 1000 characters, with at least one non-whitespace character |
| Authorization: `owner` of `GET /api/v1/user/repositories/{owner}/{repo}` | At most 39 characters; letters and digits with single inner hyphens |
| Authorization: `repo` of the same route | At most 100 characters; letters, digits, `.`, `_` and `-`, never just `.` or `..` |
| Authorization: callback `code` and `state` | At most 2048 characters |
| Authorization: admin `bootstrapSecret` / `credentialJson` | At most 256 / 16384 characters |

See [docs/target/api-contracts.md](../target/api-contracts.md) for the full intended API surface including request/response shapes.

# Target API Contracts

Status: Target

This document defines the intended API surface for the TimeForCode platform. Endpoints marked ⚠️ are stubbed in code; ❌ are not yet started. See [current API surface](../current/api-surface.md) for what exists today.

---

## General Conventions

- **Authentication**: Bearer JWT in `Authorization` header (forwarded from the HttpOnly cookie by the front-end BFF).
- **Content type**: `application/json`.
- **Versioning**: `/api/v1/` prefix.
- **Errors**: ASP.NET Core `ProblemDetails` format.
- **Identifiers**: UUID v4.
- **Pagination**: `GET` list endpoints accept `?page=1&pageSize=20`.

---

## Authorization API

### POST /api/authentication/login

Initiates GitHub OAuth 2.0 login. Returns a redirect to the GitHub authorization page.

### GET /api/authentication/callback

OAuth 2.0 callback. Exchanges the authorization code, creates an internal user, issues JWT and refresh token cookies.

### POST /api/authentication/refresh

Issues a new access token using the refresh token cookie.

### POST /api/authentication/logout

Clears both token cookies.

### GET /api/user

Returns the currently authenticated user's profile.

**Response**:

```json
{
  "id": "uuid",
  "githubUsername": "string",
  "email": "string",
  "roles": ["Contributor"]
}

```

---

## Donation API

### Projects

#### POST /api/v1/project ✅

Register a public GitHub repository as a project in state `Draft`; metadata is fetched from GitHub. Errors: `400` private or archived repository, `401` no user token, `409` repository already registered.

**Request**:

```json
{
  "githubUrl": "https://github.com/owner/repo"
}

```

**Response** (201 Created, `Location` points at `GET /api/v1/project/{id}`):

```json
{
  "projectId": "5f43a0e74b12c84f1b000001"
}
```

#### GET /api/v1/project ✅

Implemented: searches active projects by text, language and tag with paging. Query parameters are `searchText`, `language`, `tag`, `pageNumber` and `pageSize`; the response uses `projects`, `totalCount`, `pageNumber` and `pageSize`. See [project search](../current/api-surface.md#project-search) for the contract.

#### GET /api/v1/project/{id} ✅

Implemented: returns the full details of an `Active` project (`404` for any other state or an unknown id); no authentication required. See [docs/current/api-surface.md](../current/api-surface.md).

#### Project lifecycle ✅

Implemented as `POST /api/v1/project/{id}/submit`, `/approve`, `/request-changes`, `/archive` and `/reactivate`; replaces the former single `PATCH .../status` design. Maintainers read their own project in any state, including the reviewer reason, via `GET /api/v1/project/{id}/manage`. See [docs/current/api-surface.md](../current/api-surface.md) for roles, payloads and status codes.

---

### Donations

Implemented (replaces the earlier donation create, list, get and status designs): `POST` and `GET /api/v1/project/{id}/donation`, `GET /api/v1/donation`, `POST /api/v1/donation/{id}/activate` and `/cancel`. See [docs/current/api-surface.md](../current/api-surface.md#donation-endpoints).

---

### Contributions

#### Hour tracking ✅

Implemented per project, not per donation (replaces the earlier `POST` and `GET /api/v1/donation/{id}/transaction` design): `POST` and `GET /api/v1/project/{id}/contribution`, and `PUT` and `DELETE /api/v1/project/{id}/contribution/{contributionId}`. Logged hours count toward the project's `Active` donations, oldest pledge first. Corrections and soft deletes keep their history and release or re-count hours. See [docs/current/api-surface.md](../current/api-surface.md#contribution-endpoints) for payloads and status codes.

---

### Organizations

#### POST /api/v1/organization ❌

Register a donor organization.

**Request**:

```json
{
  "name": "Acme Corp",
  "industry": "Software",
  "country": "NL",
  "hoursAvailable": 200
}

```

**Response** (201 Created):

```json
{
  "id": "uuid",
  "name": "Acme Corp",
  "industry": "Software",
  "country": "NL",
  "hoursAvailable": 200,
  "hoursSpent": 0
}

```

#### GET /api/v1/organization/{id} ❌

Get organization details.

#### PUT /api/v1/organization/{id} ❌

Update organization details.

#### POST /api/v1/organization/{id}/member ❌

Add a contributor to the organization.

**Request**:

```json
{ "userId": "uuid" }

```

---

### Maintainers

#### Maintainer profile and own projects ✅

Implemented as `GET /api/v1/maintainer/me` and `GET /api/v1/maintainer/me/projects`; the profile is created when the user registers their first project. See [docs/current/api-surface.md](../current/api-surface.md#maintainer-endpoints) for payloads and status codes.

### Contributors

#### Contributor profile ✅

Implemented as `GET` and `PUT /api/v1/contributor/me`, `GET` and `PUT /api/v1/contributor/{id}` and `GET /api/v1/donororganization/{id}/contributor`; replaces the earlier `POST /api/v1/contributor` design, because the profile is created on first update. See [docs/current/api-surface.md](../current/api-surface.md#contributor-endpoints) for payloads and status codes.

---

### Reporting

#### Organization impact report and contribution history export ✅

Implemented as `GET /api/v1/reporting/donororganization/{id}/impact-report` and `GET /api/v1/reporting/donororganization/{id}/contribution-history?format=Json|Csv` in the Reporting API; replaces the earlier `GET /api/v1/organization/{id}/report` design. See [docs/current/api-surface.md](../current/api-surface.md#reporting-api) for payloads, the export schema and status codes.

#### GET /api/v1/project/{id}/report ❌

Project donation report (maintainer view).

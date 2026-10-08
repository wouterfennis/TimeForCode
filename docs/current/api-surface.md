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
| `POST` | `/api/v1/authentication/refresh` | ✅ | Issues a new access token using the refresh token |
| `POST` | `/api/v1/authentication/logout` | ✅ | Clears access and refresh token cookies |

### User Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `GET` | `/api/v1/user` | ✅ | Returns the currently authenticated user's profile |

### OpenID Connect / JWKS

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `GET` | `/.well-known/openid-configuration` | ✅ | OpenID Connect discovery document |
| `GET` | `/.well-known/jwks.json` | ✅ | Public key set for JWT validation |

---

## Donation API

Base URL (local): `http://localhost:8082`

The Donation API is responsible for projects, donations, organizations, and contributors.

### Project Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/project` | ✅ | Registers a public GitHub repository as a project in state `Draft` (user JWT required); fetches full metadata from GitHub; returns 400 for private/archived repos, 409 if the repository is already registered in any state |
| `GET` | `/api/v1/project` | ✅ | Returns a paginated list of `Active` projects; no authentication required |
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

### Donation Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/donation` | ❌ | Create a donation pledge |
| `GET` | `/api/v1/donation` | ❌ | List donations |
| `GET` | `/api/v1/donation/{id}` | ❌ | Get donation details |
| `PATCH` | `/api/v1/donation/{id}/state` | ❌ | Transition donation state |

### Organization Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/donororganization` | ✅ | Registers a donor organization; returns 400 for invalid input or 409 for a duplicate name |
| `GET` | `/api/v1/donororganization` | ✅ | Returns a paginated list of donor organizations; returns 400 for invalid pagination parameters |
| `GET` | `/api/v1/donororganization/{id}` | ✅ | Returns a donor organization's details; returns 404 when not found |
| `PUT` | `/api/v1/donororganization/{id}` | ✅ | Updates a donor organization; returns 404 when not found or 409 for a duplicate name |
| `DELETE` | `/api/v1/donororganization/{id}` | ✅ | Deletes a donor organization; returns 204 No Content on success or 404 when not found |

### Contributor Endpoints

| Method | Path | Status | Description |
| --- | --- | --- | --- |
| `POST` | `/api/v1/contributor` | ❌ | Register as a contributor |
| `GET` | `/api/v1/contributor/{id}` | ❌ | Get contributor details |

---

## API Standards

All implemented endpoints follow these conventions:

- Authentication: Bearer token (internal JWT from Authorization API) passed via cookie or `Authorization` header.
- Content type: `application/json`.
- API versioning: URL path prefix (`/api/v1/`).
- Error format: standard ASP.NET Core `ProblemDetails`.
- API documentation: Swagger UI available at `/swagger` in Development mode.

See [docs/target/api-contracts.md](../target/api-contracts.md) for the full intended API surface including request/response shapes.

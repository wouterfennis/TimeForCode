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
| `POST` | `/api/v1/project` | ✅ | Publishes a public GitHub repository as a project (JWT required); fetches full metadata from GitHub; returns 400 for private/archived repos, 409 for duplicate |
| `GET` | `/api/v1/project` | ✅ | Returns a paginated list of published (non-archived) projects; no authentication required |
| `GET` | `/api/v1/project/{id}` | ✅ | Returns full project details; no authentication required |
| `DELETE` | `/api/v1/project/{id}` | ✅ | Archives (unpublishes) a project (JWT required; owner only); returns 403 if caller is not the owner, 404 if not found |

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
| `POST` | `/api/v1/organization` | ❌ | Register a donor organization |
| `GET` | `/api/v1/organization/{id}` | ❌ | Get organization details |
| `PUT` | `/api/v1/organization/{id}` | ❌ | Update organization details |

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

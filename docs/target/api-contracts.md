# Target API Contracts

Status: Target

This document defines the intended API surface for the TimeForCode platform. Endpoints marked ⚠️ are stubbed in code; ❌ are not yet started. See [current API surface](../current/api-surface.md) for what exists today.

---

## General Conventions

- **Authentication**: Bearer JWT in `Authorization` header (forwarded from HttpOnly cookie by the Website).
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

#### POST /api/v1/project ⚠️
Register a new project.

**Request**:
```json
{
  "githubUrl": "https://github.com/owner/repo"
}
```

**Response** (201 Created):
```json
{
  "id": "uuid",
  "name": "repo",
  "description": "string",
  "language": "C#",
  "status": "PendingApproval",
  "githubUrl": "https://github.com/owner/repo"
}
```

#### GET /api/v1/project ❌
List active projects with optional filters.

**Query params**: `?language=C%23&tag=testing&page=1&pageSize=20`

**Response** (200 OK):
```json
{
  "items": [ { "id": "uuid", "name": "string", "description": "string", "language": "string", "tags": [] } ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20
}
```

#### GET /api/v1/project/{id} ❌
Get a single project's full details.

#### PATCH /api/v1/project/{id}/status ❌
Admin only. Approve or reject a project.

**Request**:
```json
{ "status": "Active" }
```

---

### Donations

#### POST /api/v1/donation ❌
Create a donation pledge.

**Request**:
```json
{
  "projectId": "uuid",
  "donorOrganizationId": "uuid",
  "hoursCommitted": 40
}
```

**Response** (201 Created):
```json
{
  "id": "uuid",
  "projectId": "uuid",
  "hoursCommitted": 40,
  "hoursLogged": 0,
  "status": "Pending",
  "createdAt": "2026-04-22T10:00:00Z"
}
```

#### GET /api/v1/donation ❌
List donations for the authenticated user's organization or as an individual.

#### GET /api/v1/donation/{id} ❌
Get full donation details including all transactions.

#### PATCH /api/v1/donation/{id}/status ❌
Transition donation status (Pending → Active, Active → Cancelled, etc.).

**Request**:
```json
{ "status": "Active" }
```

#### POST /api/v1/donation/{id}/transaction ❌
Log hours against an active donation.

**Request**:
```json
{
  "contributorId": "uuid",
  "hours": 4.5,
  "description": "Implemented feature X",
  "githubReference": "https://github.com/owner/repo/pull/42"
}
```

**Response** (201 Created):
```json
{
  "id": "uuid",
  "donationId": "uuid",
  "hours": 4.5,
  "description": "string",
  "loggedAt": "2026-04-22T14:00:00Z"
}
```

#### GET /api/v1/donation/{id}/transaction ❌
List all hour-log transactions for a donation.

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

### Contributors

#### POST /api/v1/contributor ❌
Register the current user as an independent contributor.

**Request**:
```json
{
  "role": "Backend Developer",
  "skills": ["C#", ".NET", "MongoDB"],
  "hoursAvailable": 8
}
```

#### GET /api/v1/contributor/{id} ❌
Get contributor profile.

---

### Reporting

#### GET /api/v1/organization/{id}/report ❌
Organization impact report.

**Response**:
```json
{
  "organizationId": "uuid",
  "hoursCommitted": 200,
  "hoursLogged": 87,
  "projectsSupported": 3,
  "donations": []
}
```

#### GET /api/v1/project/{id}/report ❌
Project donation report (maintainer view).

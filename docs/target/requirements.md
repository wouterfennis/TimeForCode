# Requirements

Status: Target

This document lists the functional and non-functional requirements for the TimeForCode platform. Requirements are ordered by priority within each category.

---

## Functional Requirements

### Authentication and Identity

| ID | Priority | Requirement |
| --- | --- | --- |
| AUTH-01 | Must | Users must be able to sign in using their GitHub account via OAuth 2.0 |
| AUTH-02 | Must | The platform must issue internal JWTs that downstream services validate independently |
| AUTH-03 | Must | Access tokens must expire within 60 minutes; refresh tokens within 7 days |
| AUTH-04 | Must | Logout must invalidate both the access token and refresh token |
| AUTH-05 | Should | The platform should support additional OAuth 2.0 providers (e.g. Google) in future |
| AUTH-06 | Should | Administrators must be able to revoke user sessions |

### Project Management

| ID | Priority | Requirement |
| --- | --- | --- |
| PROJ-01 | Must | Project maintainers must be able to register a project by submitting a GitHub repository URL |
| PROJ-02 | Must | The platform must fetch project metadata (name, description, language, open issues) from the GitHub API at registration time |
| PROJ-03 | Must | Registered projects must be reviewed and approved by an administrator before becoming publicly visible |
| PROJ-04 | Must | Projects must be searchable and filterable by language, tags, and needed skills |
| PROJ-05 | Must | Project maintainers must be able to define milestones and link them to GitHub milestones |
| PROJ-06 | Should | Project metadata should be periodically re-synced from GitHub |
| PROJ-07 | Could | Projects could be featured or spotlighted via admin-curated campaigns |

### Donations

| ID | Priority | Requirement |
| --- | --- | --- |
| DON-01 | Must | Donor organizations must be able to pledge a specific number of developer hours to a project |
| DON-02 | Must | Individual donors must be able to pledge personal hours independently |
| DON-03 | Must | Donations must have a defined lifecycle: Pending → Active → Completed or Cancelled |
| DON-04 | Must | Donors must be able to view all their donations and their current status |
| DON-05 | Must | Donors must be able to cancel a Pending donation |
| DON-06 | Should | Donors should receive notifications when a donation transitions to Active |
| DON-07 | Should | Donations should support partial completion (hours logged < hours pledged) |

### Hour Tracking

| ID | Priority | Requirement |
| --- | --- | --- |
| TRACK-01 | Must | Contributors must be able to log hours against an active donation |
| TRACK-02 | Must | Each hour-log entry must include a description and a timestamp |
| TRACK-03 | Must | The platform must calculate and display progress toward the pledged total |
| TRACK-04 | Should | A donation should automatically transition to Completed when pledged hours are fully logged |
| TRACK-05 | Should | Hour-log entries should be linkable to a specific GitHub commit or pull request |

### Organizations and Contributors

| ID | Priority | Requirement |
| --- | --- | --- |
| ORG-01 | Must | Companies must be able to register as donor organizations with a name, industry, and country |
| ORG-02 | Must | Organizations must be able to define a total pool of available donor hours |
| ORG-03 | Must | Organizations must be able to assign employees as contributors to specific donations |
| ORG-04 | Should | Individual developers must be able to register as independent contributors |
| ORG-05 | Should | Contributors should be able to list their skills and preferred technologies |

### Matchmaking

| ID | Priority | Requirement |
| --- | --- | --- |
| MATCH-01 | Should | The platform should suggest relevant projects to donors based on their technology stack and skills |
| MATCH-02 | Should | The platform should surface donors to project maintainers based on project needs |
| MATCH-03 | Could | Campaigns could group projects by theme (e.g. security, accessibility) to help donors choose |

### Reporting and Impact

| ID | Priority | Requirement |
| --- | --- | --- |
| RPT-01 | Must | Organizations must be able to view a summary of all their donations and hours contributed |
| RPT-02 | Must | Project maintainers must be able to view all donations received and hours logged |
| RPT-03 | Should | Organizations should be able to export their contribution history |
| RPT-04 | Should | The platform should award badges to organizations based on contribution milestones |
| RPT-05 | Could | A public leaderboard could display top contributing organizations |

---

## Non-Functional Requirements

### Security

| ID | Priority | Requirement |
| --- | --- | --- |
| SEC-01 | Must | All API endpoints must require authentication except for public project listings and the login flow |
| SEC-02 | Must | Tokens must be stored in HttpOnly, Secure cookies — not in browser local storage |
| SEC-03 | Must | Redirect URIs must be validated against an explicit allowlist |
| SEC-04 | Must | All service-to-service communication must validate the JWT audience and issuer |
| SEC-05 | Must | Sensitive configuration (client secrets, signing keys, database credentials) must be stored in a secrets manager, not in source code or plain environment variables |
| SEC-06 | Should | The platform should enforce HTTPS in all environments |

### Reliability and Availability

| ID | Priority | Requirement |
| --- | --- | --- |
| REL-01 | Should | The platform should target 99.5% uptime for the Authorization API |
| REL-02 | Should | The platform should degrade gracefully when the GitHub API is unavailable |
| REL-03 | Could | Critical operations (donation state transitions) could use idempotency keys to prevent duplicates |

### Performance

| ID | Priority | Requirement |
| --- | --- | --- |
| PERF-01 | Should | Project listing API responses should be returned within 500ms under normal load |
| PERF-02 | Should | Authentication flows should complete within 3 seconds end-to-end |
| PERF-03 | Could | Heavy read operations (reporting, project search) could be served from a read-optimised cache |

### Maintainability

| ID | Priority | Requirement |
| --- | --- | --- |
| MAINT-01 | Must | All new bounded contexts must follow the same Domain → Application → Infrastructure → API layering pattern |
| MAINT-02 | Must | Architecture tests must be added for every new bounded context |
| MAINT-03 | Should | API endpoints must be documented via OpenAPI/Swagger |
| MAINT-04 | Should | Architecture decisions must be recorded as ADRs |

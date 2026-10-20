# Capability Status

Status: Current

This document provides an explicit, feature-level view of what is implemented, partially implemented, or not yet started.

| ✅ = Done | ⚠️ = Partial / Stubbed | ❌ = Not started |

---

## Authentication and Identity

| Capability | Status | Notes |
| --- | --- | --- |
| GitHub OAuth 2.0 login | ✅ | Full flow implemented |
| Internal JWT issuance (RS256) | ✅ | Access token + refresh token |
| Refresh token rotation | ✅ | 7-day expiry, rotated on use; only a SHA-256 hash is stored; replaying a rotated token revokes all of the user's refresh tokens; records are removed by a TTL index on expiry; admin sessions refresh to an `admin` scope token |
| Secure cookie storage | ✅ | HttpOnly, Secure cookies |
| Logout | ✅ | Clears tokens |
| User account creation on first login | ✅ | Linked to GitHub identity |
| Delete account and personal data | ✅ | `DELETE /api/v1/user` removes the profile and refresh tokens and anonymises the contributor in the Donation module; stored data is listed in [personal data](../reference/personal-data.md). No data export yet |
| Multiple identity providers (e.g. Google) | ❌ | Architecture supports it; not configured |
| Admin identity management | ❌ | No admin account management |

---

## Project Management

| Capability | Status | Notes |
| --- | --- | --- |
| Register a project (API endpoint) | ✅ | POST /api/v1/project fully implemented; fetches full GitHub metadata on publish |
| Fetch project metadata from GitHub | ✅ | GithubRepositoryApiService calls the GitHub REST API on publish; locally routed to the Identity Provider Mock |
| List projects | ✅ | GET /api/v1/project with pagination; lists `Active` projects only; no authentication required |
| Search and filter projects | ✅ | Same endpoint with `searchText`, `language` and `tag` filters (combinable), newest-first sort and Mongo indexes; back-end only, see [project search](api-surface.md#project-search) |
| Project lifecycle (submit, approve, request changes, archive, re-activate) | ✅ | Back-end only; maintainers read their own non-Active project and the reviewer reason via `GET /api/v1/project/{id}/manage`; states Draft, PendingApproval, Active, Archived; approve and request-changes are admin-only, the rest maintainer-only; the `Active`-only donation rule is enforced in the domain but no donation endpoint exists yet |
| Update project details | ❌ | No endpoint |
| Project milestone tracking | ❌ | Domain model exists; no API |
| Project tagging | ❌ | Domain model exists; no API |

---

## Donations

| Capability | Status | Notes |
| --- | --- | --- |
| Create a donation pledge | ✅ | `POST /api/v1/project/{id}/donation`, back-end only; `Active` projects only; donor organization XOR individual |
| List donations for a project | ✅ | `GET /api/v1/project/{id}/donation` (anonymous) and `GET /api/v1/donation` (own donations), with hours committed, hours logged and progress |
| Donation state transitions (Pending → Active → Completed) | ✅ | Activate and cancel by the donor or an administrator; completion is automatic when logged hours reach the commitment |
| Hour allocation per contributor | ❌ | Logged hours count toward the project's Active donations (oldest first), not per contributor |
| Hour completion tracking | ✅ | `hoursLogged` and `progressPercentage` per donation; donor organizations show `hoursSpent` and `hoursAvailable` |
| Cancel or withdraw a donation | ✅ | `POST /api/v1/donation/{id}/cancel` while Pending or Active |

---

## Organizations and Contributors

| Capability | Status | Notes |
| --- | --- | --- |
| Register a donor organization | ❌ | Domain model exists; no API |
| Manage organization employees | ❌ | Not implemented |
| Register as an individual contributor | ✅ | `PUT /api/v1/contributor/me` creates the contributor on first use; logging hours creates it too, and both reuse the single contributor per platform user |
| Contributor skill profile | ✅ | Role, normalised skills, hours available and optional donor organization link; own profile or administrators only; administrators list the contributors of a donor organization. Matchmaking does not use it yet |
| View available donated hours | ❌ | Not implemented |

---

## Matchmaking

| Capability | Status | Notes |
| --- | --- | --- |
| Match donated hours to project needs | ❌ | No implementation |
| Suggest projects based on skills | ❌ | No implementation |
| Campaign and spotlight features | ❌ | Concept only |

---

## Reporting and Impact

| Capability | Status | Notes |
| --- | --- | --- |
| Hours donated per organization | ❌ | Not implemented |
| Hours spent per project | ⚠️ | Contributions can be logged and listed per project (`POST`/`GET /api/v1/project/{id}/contribution`); no total field or dashboard yet |
| Contribution history for a user | ❌ | Not implemented |
| Impact dashboard | ❌ | Not implemented |
| Badges and tiers | ❌ | Concept only |

---

## React front end

| Capability | Status | Notes |
| --- | --- | --- |
| SPA scaffold (Vite, React, TypeScript) | ⚠️ | Placeholder page only |
| Front-end quality gates in CI | ✅ | Lint (oxlint), typecheck, Vitest with axe, coverage to SonarCloud and build run in the `frontend` job |
| Login, project and donation screens | ❌ | Not implemented |
| Donation pledge form | ❌ | Not implemented |
| Organization profile page | ❌ | Not implemented |
| Contributor profile page | ❌ | Not implemented |
| Impact dashboard | ❌ | Not implemented |
| Success stories | ❌ | Concept only |
| Forum | ❌ | Concept only |

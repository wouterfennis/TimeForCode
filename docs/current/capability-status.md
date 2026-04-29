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
| Refresh token rotation | ✅ | 7-day expiry, rotated on use |
| Secure cookie storage | ✅ | HttpOnly, Secure cookies |
| Logout | ✅ | Clears tokens |
| User account creation on first login | ✅ | Linked to GitHub identity |
| Multiple identity providers (e.g. Google) | ❌ | Architecture supports it; not configured |
| Admin identity management | ❌ | No admin account management |

---

## Project Management

| Capability | Status | Notes |
| --- | --- | --- |
| Register a project (API endpoint) | ⚠️ | Route exists; logic not implemented |
| Fetch project metadata from GitHub | ⚠️ | Sequence designed; not implemented |
| List projects | ❌ | No endpoint |
| Search and filter projects | ❌ | No endpoint |
| Update project details | ❌ | No endpoint |
| Project milestone tracking | ❌ | Domain model exists; no API |
| Project tagging | ❌ | Domain model exists; no API |

---

## Donations

| Capability | Status | Notes |
| --- | --- | --- |
| Create a donation pledge | ❌ | Domain model exists; no API |
| List donations for a project | ❌ | No endpoint |
| Donation state transitions (Pending → Active → Completed) | ❌ | State machine not implemented |
| Hour allocation per contributor | ❌ | Not implemented |
| Hour completion tracking | ❌ | Not implemented |
| Cancel or withdraw a donation | ❌ | Not implemented |

---

## Organizations and Contributors

| Capability | Status | Notes |
| --- | --- | --- |
| Register a donor organization | ❌ | Domain model exists; no API |
| Manage organization employees | ❌ | Not implemented |
| Register as an individual contributor | ❌ | Not implemented |
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
| Hours spent per project | ❌ | Not implemented |
| Contribution history for a user | ❌ | Not implemented |
| Impact dashboard | ❌ | Not implemented |
| Badges and tiers | ❌ | Concept only |

---

## Website UI

| Capability | Status | Notes |
| --- | --- | --- |
| Login / logout | ⚠️ | Auth integration present; UI minimal |
| Project listing page | ❌ | Not implemented |
| Project detail page | ❌ | Not implemented |
| Donation pledge form | ❌ | Not implemented |
| Organization profile page | ❌ | Not implemented |
| Contributor profile page | ❌ | Not implemented |
| Impact dashboard | ❌ | Not implemented |
| Success stories | ❌ | Concept only |
| Forum | ❌ | Concept only |

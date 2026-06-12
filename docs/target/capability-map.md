# Capability Map

Status: Mixed

This document maps every target platform capability against its current implementation status. Use this as the gap analysis for planning implementation work.

| Status | Meaning |
| --- | --- |
| ✅ Done | Implemented and tested |
| ⚠️ Partial | Started but incomplete |
| ❌ Not started | Defined in requirements; not yet implemented |

---

## Authentication and Identity

| Capability | Current | Target | Gap |
| --- | --- | --- | --- |
| GitHub OAuth 2.0 login | ✅ | ✅ | None |
| Internal JWT (RS256) | ✅ | ✅ | None |
| Refresh token rotation | ✅ | ✅ | None |
| Secure cookie storage | ✅ | ✅ | None |
| Logout | ✅ | ✅ | None |
| User account creation | ✅ | ✅ | None |
| Additional OAuth providers | ❌ | ⚠️ Should | Architecture supports it; not configured |
| Admin session revocation | ❌ | ⚠️ Should | Not implemented |

---

## Project Management

| Capability | Current | Target | Gap |
| --- | --- | --- | --- |
| Register project via GitHub URL | ⚠️ Stub | ✅ Must | Implement handler, GitHub fetch, persistence |
| Admin approval workflow | ❌ | ✅ Must | Full workflow needed |
| List and search projects | ❌ | ✅ Must | Query endpoint needed |
| Project detail view | ❌ | ✅ Must | Endpoint and UI needed |
| Milestone tracking | ❌ | ✅ Must | Domain model exists; API needed |
| Tagging and filtering | ❌ | ✅ Must | Not started |
| GitHub re-sync | ❌ | ⚠️ Should | Periodic sync mechanism needed |
| Campaign / spotlight | ❌ | ⚠️ Could | Not started |

---

## Donations

| Capability | Current | Target | Gap |
| --- | --- | --- | --- |
| Create donation pledge | ❌ | ✅ Must | Domain model exists; API and handler needed |
| Donation lifecycle (state machine) | ❌ | ✅ Must | Pending → Active → Completed / Cancelled |
| List donations | ❌ | ✅ Must | Not started |
| Cancel pending donation | ❌ | ✅ Must | Not started |
| Partial completion support | ❌ | ⚠️ Should | Not started |
| Notifications on state change | ❌ | ⚠️ Should | Not started |

---

## Hour Tracking

| Capability | Current | Target | Gap |
| --- | --- | --- | --- |
| Log hours against a donation | ❌ | ✅ Must | DonationTransaction domain exists; API needed |
| Progress tracking | ❌ | ✅ Must | Calculation logic needed |
| Auto-complete at 100% | ❌ | ⚠️ Should | Business rule needed |
| Link hours to GitHub commits/PRs | ❌ | ⚠️ Should | Not started |

---

## Organizations and Contributors

| Capability | Current | Target | Gap |
| --- | --- | --- | --- |
| Register donor organization | ❌ | ✅ Must | Domain model exists; API needed |
| Manage available hours | ❌ | ✅ Must | Not started |
| Assign contributors to donations | ❌ | ✅ Must | Not started |
| Individual contributor registration | ❌ | ⚠️ Should | Not started |
| Contributor skill profiles | ❌ | ⚠️ Should | Not started |

---

## Matchmaking

| Capability | Current | Target | Gap |
| --- | --- | --- | --- |
| Suggest projects to donors | ❌ | ⚠️ Should | Not started |
| Surface donors to maintainers | ❌ | ⚠️ Should | Not started |
| Themed campaigns | ❌ | ⚠️ Could | Not started |

---

## Reporting and Impact

| Capability | Current | Target | Gap |
| --- | --- | --- | --- |
| Organization contribution summary | ❌ | ✅ Must | Not started |
| Project donation received report | ❌ | ✅ Must | Not started |
| Export contribution history | ❌ | ⚠️ Should | Not started |
| Badges and tiers | ❌ | ⚠️ Should | Not started |
| Public leaderboard | ❌ | ⚠️ Could | Not started |

---

## Website UI

| Capability | Current | Target | Gap |
| --- | --- | --- | --- |
| Login / logout | ⚠️ | ✅ Must | UI work needed |
| Project listing page | ❌ | ✅ Must | Not started |
| Project detail page | ❌ | ✅ Must | Not started |
| Donation pledge form | ❌ | ✅ Must | Not started |
| Organization profile / dashboard | ❌ | ✅ Must | Not started |
| Contributor profile | ❌ | ⚠️ Should | Not started |
| Impact dashboard | ❌ | ✅ Must | Not started |
| Success stories | ❌ | ⚠️ Could | Not started |
| Forum | ❌ | ⚠️ Could | Not started |

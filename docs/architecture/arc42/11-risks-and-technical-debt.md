# Arc42 Section 11 — Risks and Technical Debt

Status: Current

This section tracks known risks and technical debt that could affect the platform's quality, delivery, or security.

---

## Risks

| ID | Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- | --- |
| RISK-01 | GitHub API rate limiting blocks project registration | Medium | Medium | Cache metadata after first fetch; implement exponential backoff; use authenticated requests to raise rate limit |
| RISK-02 | GitHub as single identity provider creates a hard dependency | Low | High | Architecture already supports multiple providers; adding a second provider is a configuration change, not a rewrite |
| RISK-03 | MongoDB schema drift as domain evolves | Medium | Medium | Use versioned document schemas or migration scripts; add integration tests that validate schema compatibility |
| RISK-04 | Hardcoded RSA key material in local config could be accidentally deployed | Low | High | CI/CD pipeline must never use `docker-compose.yaml` values in non-local environments; Key Vault integration is required before production |
| RISK-05 | Donation hour logging has no concurrency protection today | Medium | High | Implement optimistic locking or atomic MongoDB updates before enabling concurrent contributors on a single donation |
| RISK-06 | No staging environment means risky direct-to-production deployments | Medium | High | Define a staging environment in Bicep and require deployment through staging before production |
| RISK-07 | The privacy documentation ([personal data](../../reference/personal-data.md)) has had no legal check: lawful basis, retention periods and the controller information are not confirmed | Medium | High | Have the wording reviewed before the first production release; adjust purposes and retention to the outcome |
| RISK-08 | Personal data remains after account deletion: `PublishedByUserId` on the projects of the user (no longer resolves to a profile), an access token issued before the deletion stays valid for up to 60 minutes and can still log hours, which creates a new contributor with the login of the token, user ids in logs, and database backups | Medium | Low | Accepted. Access tokens are short-lived; the leftover identifier cannot be traced to a person without the deleted profile. Revisit with a token revocation list or a deletion check in the Donation API if this is not acceptable |
| RISK-09 | Account deletion depends on the Donation API: while it is down the deletion fails with `502`, and a failure after the anonymisation step leaves an anonymised contributor with a live account until the user retries | Low | Medium | The order is anonymise, delete tokens, delete the account, and every step is idempotent, so a retry completes the deletion; the user is told to try again later |

---

## Technical Debt

| ID | Item | Affected Area | Impact | Effort to Resolve |
| --- | --- | --- | --- | --- |
| DEBT-01 | Tech stack in README was T.B.D. | Repository entry point | Stale onboarding experience | Fixed — README updated |
| DEBT-02 | Authorization API does not use URL-versioned routes | Authorization API | Inconsistency with Donation API; harder to version in future | Low effort; add `/api/v1/` prefix |
| DEBT-03 | No secrets manager in production | Authorization API | Secrets passed as App Settings are visible in Azure Portal | Implement Key Vault references in Bicep |
| DEBT-04 | Only Authorization API deployed to Azure; Donation API and Website are not | Deployment | Platform cannot be tested end-to-end in cloud | Extend Bicep templates for remaining services |
| DEBT-05 | Donation lifecycle and hour tracking are not implemented | Donation bounded context | Donor-organization CRUD and project registration are implemented, but donation pledging and time tracking remain absent | Core implementation work in Phase 2 |
| DEBT-06 | No E2E tests for the Website | Website | Login and donation flows not verified at UI level | Add Playwright tests in a future phase |
| DEBT-07 | No structured logging aggregation in production | Operations | Debugging production issues requires manual log inspection | Add Application Insights or Azure Monitor integration |
| DEBT-08 | GitHub client secret visible in `docker-compose.yaml` and `application.bicep` | Security | Credentials in repository history | Rotate secret; move to Key Vault; scrub git history if needed |
| DEBT-09 | No architecture tests enforce cross-context isolation (Authorization ↔ Donation) | Both bounded contexts | A future accidental import of an Authorization type in Donation (or vice versa) would not be caught by CI | Fixed — `CrossContextIsolationTests` added to Donation.Architecture.Tests |
| DEBT-10 | Project text search uses an unanchored case-insensitive regex on name, full name and description | Donation Infrastructure (`ProjectSearchDefinitions`) | The regex cannot use an index; the status prefix of the compound indexes only narrows the scan to `Active` projects, so cost grows with the number of active projects | Introduce a text index or Atlas Search when the catalogue grows beyond a few thousand active projects; consider a minimum `searchText` length of 2 to 3 characters. Mitigated meanwhile by a 5 second server-side time limit and cancellation on the count and find calls |
| DEBT-11 | The Website is deprecated (2026-10-13) and no longer compiles against the Donation API client (`GetProjectsAsync` gained search parameters) | Website | Removed from `TimeForCode.sln` and the CI workflows, see `src/Website/DEPRECATED.md`; its code, `Dockerfile.website` and the local compose entries remain and are unmaintained | Delete the Website projects and compose entries, or build a replacement front-end |
| DEBT-12 | The `api` rate limit is one global sliding window (60 requests per minute) with no partition key | Shared API setup (`ServiceCollectionExtensions`), Donation `Startup` | Anonymous callers share one budget: a single client can exhaust it for everyone, including the authenticated endpoints that use the same policy. Found in the security review of #76 | Partition the limiter by client IP (honouring forwarded headers behind the proxy) or user id; optionally give the search endpoint its own tighter policy |
| DEBT-13 | Project search paging uses `Skip`, and Mongo indexes are created in the `ProjectRepository` constructor | Donation Infrastructure | Deep pages (up to 10000 x 100) skip up to about 1 million documents; index creation failures surface as a 500 on the first request. Found in the security review of #76 | Tighten `MaxPageNumber` or move to keyset paging on (`PublishedAt`, `_id`) when the active set grows; create indexes at application startup |
| DEBT-14 | The Data Protection key ring is stored unencrypted in MongoDB | Authorization Infrastructure | Anyone with read access to the database can read the keys and decrypt or forge admin ceremony state (short-lived, low value) | Protect the keys at rest with a certificate or Key Vault |
| DEBT-15 | Public repository lists are limited to one page of 100 owned repositories, and the application's GitHub rate limit has not been verified against real GitHub | Authorization Infrastructure (`GithubService`) | Users with more than 100 repositories see a truncated list; many users could exhaust the limit | Follow GitHub's `Link` header; add caching or an app-installation token if the limit is hit |
| DEBT-16 | Refresh token indexes are created in the `RefreshTokenRepository` constructor, and the atomic rotation (`TryMarkRotatedAsync`) and the TTL index are only verified by definition tests, not against a real MongoDB | Authorization Infrastructure | The unique `TokenHash` index cannot be built while old plaintext `RefreshToken` documents exist, so the first request after deployment fails with a 500 (ADR-014: no migration, drop the `RefreshToken` collection before deploying #104). A regression in the conditional update or index would only show in a real database. Found in the review of #104 | Drop the collection before deploying; create indexes at application startup; add an integration test against a MongoDB container when one is available |
| DEBT-17 | `DonationOptions__BaseUri` is set in the local compose file and `appsettings.Development.json` only; `application.bicep` has no value because the Donation API is not deployed (DEBT-04) | Authorization deployment (`deploy/`) | Account deletion fails with `502` in the cloud environment until the Donation API is deployed and the setting added | Add the setting to the Bicep template together with the Donation API deployment |

> **Note on DEBT-08**: The credential visible in `application.bicep` should be treated as compromised and rotated. A Key Vault reference must be used for production deployments.

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

---

## Technical Debt

| ID | Item | Affected Area | Impact | Effort to Resolve |
| --- | --- | --- | --- | --- |
| DEBT-01 | Tech stack in README was T.B.D. | Repository entry point | Stale onboarding experience | Fixed — README updated |
| DEBT-02 | Authorization API does not use URL-versioned routes | Authorization API | Inconsistency with Donation API; harder to version in future | Low effort; add `/api/v1/` prefix |
| DEBT-03 | No secrets manager in production | Authorization API | Secrets passed as App Settings are visible in Azure Portal | Implement Key Vault references in Bicep |
| DEBT-04 | Only Authorization API deployed to Azure; Donation API and Website are not | Deployment | Platform cannot be tested end-to-end in cloud | Extend Bicep templates for remaining services |
| DEBT-05 | Donation lifecycle (donations, hour tracking, organisations) not implemented | Donation bounded context | Core platform feature gap; project registration is done but donation pledging and time tracking are absent | Core implementation work in Phase 2 |
| DEBT-06 | No E2E tests for the Website | Website | Login and donation flows not verified at UI level | Add Playwright tests in a future phase |
| DEBT-07 | No structured logging aggregation in production | Operations | Debugging production issues requires manual log inspection | Add Application Insights or Azure Monitor integration |
| DEBT-08 | GitHub client secret visible in `docker-compose.yaml` and `application.bicep` | Security | Credentials in repository history | Rotate secret; move to Key Vault; scrub git history if needed |

> **Note on DEBT-08**: The credential visible in `application.bicep` should be treated as compromised and rotated. A Key Vault reference must be used for production deployments.

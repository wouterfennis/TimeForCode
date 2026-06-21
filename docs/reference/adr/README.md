# Architecture Decision Records

This folder contains Architecture Decision Records (ADRs) for the TimeForCode platform. Each ADR captures the context, the decision made, and the consequences of that decision.

ADRs are referenced from [Arc42 Section 09 — Architecture Decisions](../architecture/arc42/09-architecture-decisions.md).

---

## Index

| ID | Title | Status |
| --- | --- | --- |
| [ADR-001](#adr-001) | Use internal JWT instead of forwarding GitHub tokens | Accepted |
| [ADR-002](#adr-002) | Use MongoDB as the primary data store | Accepted |
| [ADR-003](#adr-003) | Use Arc42 for architecture documentation | Accepted |
| [ADR-004](#adr-004) | Use MediatR for command dispatch | Accepted |
| [ADR-005](#adr-005) | Store tokens in HttpOnly cookies, not localStorage | Accepted |
| [ADR-006](#adr-006) | Use Reqnroll + BDD for acceptance tests | Accepted |
| [ADR-007](#adr-007) | Swagger snapshot gate for API contract review | Accepted |
| [ADR-008](#adr-008) | Exclude E2E tests from CI | Accepted |

---

## ADR Template

When adding a new ADR, use this structure:

```markdown
## ADR-NNN — Title

**Date**: YYYY-MM-DD
**Status**: Proposed | Accepted | Deprecated | Superseded by ADR-NNN

### Context
What is the situation or problem that requires a decision?

### Decision
What decision was made?

### Consequences
What are the positive and negative consequences of this decision?
```

---

## ADR-001

**Date**: 2025
**Status**: Accepted

### Context

TimeForCode uses GitHub OAuth 2.0 for authentication. After login, downstream services (e.g. the Donation API) need to authenticate requests from the Website. The question was whether to forward the GitHub access token to downstream services or issue an internal token.

### Decision

The Authorization API issues its own internal JWT (RS256-signed) after a successful GitHub login. Downstream services validate this internal token. The GitHub access token is never forwarded outside the Authorization API.

### Consequences

**Positive**: Downstream services are decoupled from GitHub. Adding a second identity provider (e.g. Google) requires no changes to downstream services. Internal claims (roles, user ID) are controlled by the platform, not GitHub.

**Negative**: An additional token issuance step adds complexity to the login flow. The Authorization API becomes a trust anchor that must be kept secure.

---

## ADR-002

**Date**: 2025
**Status**: Accepted

### Context

The platform needs a data store that can handle evolving schemas as the domain model grows, and must be straightforward to run locally in a Docker container.

### Decision

MongoDB is used as the primary data store for all services. Each bounded context owns its own MongoDB database.

### Consequences

**Positive**: Schema flexibility suits a rapidly evolving domain. Strong .NET driver support. Easy to run locally with Docker. Azure Cosmos DB for MongoDB API provides a managed cloud option.

**Negative**: Joins across contexts must be done in application code, not the database. Transactions are supported but more complex than in relational databases.

---

## ADR-003

**Date**: 2026
**Status**: Accepted

### Context

The platform needs an architecture documentation approach that can serve both human developers and AI agents building the system over time. Documentation must be structured, discoverable, and maintained alongside code.

### Decision

Arc42 is used as the architecture documentation template. All 12 Arc42 sections are maintained in `docs/architecture/arc42/`. Architecture changes require updating the relevant section(s) before merging.

### Consequences

**Positive**: Standardised structure that is well-known in the industry. Provides a clear home for every type of architecture information. AI agents can reliably navigate a known template.

**Negative**: Requires discipline to keep sections up to date. Empty or stale sections are worse than no documentation.

---

## ADR-004

**Date**: 2025
**Status**: Accepted

### Context

The Application layer needs a way to dispatch commands and decouple the API layer from the implementation of business logic without introducing a full event-driven architecture.

### Decision

MediatR is used for in-process command dispatch. Each operation is modelled as a command (e.g. `LoginCommand`) handled by a corresponding handler. No event sourcing or external messaging is used at this stage.

### Consequences

**Positive**: Clean separation between intent (command) and execution (handler). Handlers are independently testable. Easy to add cross-cutting concerns (logging, validation) as pipeline behaviours.

**Negative**: In-process only; does not support distributed scenarios. If distributed commands are needed in future, MediatR would need to be complemented with a message broker.

---

## ADR-005

**Date**: 2025
**Status**: Accepted

### Context

JWTs must be stored in the browser after login. The choices were localStorage, sessionStorage, or HttpOnly cookies.

### Decision

Tokens are stored exclusively in HttpOnly, Secure cookies. The access token and refresh token are both set as HttpOnly cookies by the Authorization API after a successful login.

### Consequences

**Positive**: Tokens cannot be accessed by JavaScript, preventing XSS-based token theft. Cookies are automatically sent with requests to the same origin.

**Negative**: Requires careful `SameSite` and CORS configuration. CSRF protection must be considered for state-changing API calls.

---

## ADR-006

**Date**: 2026-06-21
**Status**: Accepted

### Context

The project needed an acceptance-test approach that can be understood by non-developers (product owners, AI agents), stays coupled to the feature specification throughout the lifetime of the feature, and produces human-readable output in CI logs.

The main alternatives considered were:

- Plain xUnit/MSTest integration tests (code-only, no readable specification)
- Reqnroll / SpecFlow with Gherkin scenarios (code + specification in `.feature` files)
- Postman / Newman collections (JSON-based, hard to version alongside code)

### Decision

[Reqnroll](https://reqnroll.net/) (the community continuation of SpecFlow) is used as the BDD framework for all acceptance-test projects (`*.Specifications`). Feature files are written in Gherkin and committed alongside the step definitions and production code.

Step text follows a persona convention — subjects are "The user", "The external platform", or "The time for code platform" — to keep scenarios free of implementation detail.

### Consequences

**Positive**: Feature files serve as living documentation that is always in sync with the test suite. AI agents (feature-writer, implementation, review) can read and produce Gherkin unambiguously. Scenarios map directly to acceptance-criteria checkboxes on GitHub Issues.

**Negative**: Requires discipline to keep step definitions DRY. Gherkin can become verbose for data-heavy scenarios; `Scenario Outline` + `Examples` tables mitigate this.

---

## ADR-007

**Date**: 2026
**Status**: Accepted

### Context

The Authorization and Donation APIs expose an OpenAPI (Swagger) specification. Without a gate, internal refactors can silently alter the public API surface — breaking generated clients or external callers — without any code review step.

### Decision

Each `*.Api.Tests` project contains a single snapshot test (`SwaggerTests.cs`) that generates the OpenAPI JSON at test time and compares it against a committed `.verified.txt` file using the [Verify](https://github.com/VerifyTests/Verify) library. A mismatch fails the test and writes a `.received.txt` diff file.

This failure is **intentional**: it forces every API surface change to be reviewed before the snapshot is updated. The snapshot update procedure is:

```powershell
Copy-Item `
  tst/<Module>/TimeForCode.<Module>.Api.Tests/SwaggerTests.Verify_GeneratedSwaggerFile_ShouldNotChangeUnlessIntended.received.txt `
  tst/<Module>/TimeForCode.<Module>.Api.Tests/SwaggerTests.Verify_GeneratedSwaggerFile_ShouldNotChangeUnlessIntended.verified.txt `
  -Force
```

The updated `.verified.txt` must be committed alongside the code change in the same pull request so the diff is reviewable.

### Consequences

**Positive**: Breaking changes to the public API surface are caught immediately in CI. Reviewers can see the exact OpenAPI diff in the pull request. The NSwag-generated client (`TimeForCode.Authorization.Api.Client`) is naturally included in the review checklist.

**Negative**: The snapshot file must be updated manually on every intentional API change. Developers unfamiliar with the pattern may be confused by a failing test that is expected to fail.

---

## ADR-008

**Date**: 2026
**Status**: Accepted

### Context

The `TimeForCode.Website.Specifications` project contains browser-level E2E tests powered by Microsoft Playwright. These tests require the full Docker Compose stack to be running (Website, Authorization API, Donation API, Identity Provider Mock, MongoDB). Running the stack in CI requires container-in-container support and significantly increases build times.

### Decision

All scenarios in `TimeForCode.Website.Specifications` are tagged `@E2E`. The CI pipeline passes `--filter "TestCategory!=E2E"` to `dotnet test`, so E2E tests are **never executed during automated builds**.

E2E tests are run manually against a locally started stack before significant releases or when changes affect the Website or the cross-service authentication flow.

**Manual run procedure:**

```powershell
# 1. Start the full stack
.\scripts\start-local.ps1

# 2. Install Playwright browser binaries (first time only)
pwsh tst/Website/TimeForCode.Website.Specifications/bin/Debug/net10.0/playwright.ps1 install chromium

# 3. Run the E2E suite
dotnet test tst/Website/TimeForCode.Website.Specifications/
```

### Consequences

**Positive**: CI remains fast. E2E failures do not block unrelated pull requests. Developers without a full local stack are not blocked.

**Negative**: E2E regressions may go undetected between manual runs. The CI suite does not catch issues that only appear at the full-stack integration level (e.g. cross-service cookie handling, OAuth redirect flows).

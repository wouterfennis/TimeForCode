# Arc42 Section 2 — Architecture Constraints

Status: Current

This section lists all constraints that are not negotiable. They must be respected when making architecture decisions.

---

## Technical Constraints

| Constraint | Rationale |
|---|---|
| Backend must use .NET (ASP.NET Core) | Existing codebase and team expertise. |
| Frontend must use Blazor | Existing codebase; allows sharing .NET types across client and server. |
| Storage must use MongoDB | Existing decision; document model suits the domain's flexible schemas. |
| Containerisation with Docker | Local development and cloud deployment both rely on container images. |
| Infrastructure as Code using Azure Bicep | Deployment must be reproducible and auditable. |
| Deployment target is Microsoft Azure | Cloud provider selected; services run on Azure App Service. |
| CI/CD via GitHub Actions | Pipeline is already in place; test and quality gates run on every pull request. |
| Code quality gate via SonarCloud | All pull requests must pass the SonarCloud quality gate. |

---

## Architectural Constraints

| Constraint | Rationale |
|---|---|
| Each bounded context must follow Domain → Application → Infrastructure → API layering | Enforced by Architecture.Tests in each context; prevents circular dependencies. |
| Architecture tests are mandatory for every new bounded context | Prevents layering violations from silently accumulating. |
| Only the Authorization API may issue tokens | All other services validate tokens; none of them sign or issue new ones. |
| Tokens must be stored in HttpOnly, Secure cookies on the browser | Prevents XSS-based token theft. |
| Secrets must not be committed to source control | GitHub Actions secrets and Azure Key Vault should be used in all non-local environments. |
| Redirect URIs must be explicitly allowlisted | Prevents open redirect attacks in the OAuth 2.0 flow. |

---

## Organisational Constraints

| Constraint | Rationale |
|---|---|
| Project is open source (MIT licence) | All dependencies must be compatible with the MIT licence. |
| Documentation must be maintained alongside code | The `docs/` folder is the anchor for requirements and architecture; docs drift is not acceptable. |
| Any significant architecture decision must be recorded as an ADR | Provides a traceable history of decisions for future developers and AI agents. |

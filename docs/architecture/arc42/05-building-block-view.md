# Arc42 Section 5 — Building Block View

Status: Mixed

This section describes the static decomposition of the system into building blocks and their responsibilities.

Published views of the [LikeC4 architecture model](../likec4/README.md): [containers](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/containers/), [front-end](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/frontend/), [Authorization](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/authorization/), [Donation](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/donation/) and [Shared](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/shared/); per layer [Authorization API](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/authorizationApi/), [Authorization Application](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/authorizationApplication/), [Authorization Infrastructure](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/authorizationInfrastructure/), [Donation API](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/donationApi/), [Donation Application](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/donationApplication/), [Donation Domain](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/donationDomain/) and [Donation Infrastructure](https://wouterfennis.github.io/TimeForCode/architecture-model/#/view/donationInfrastructure/).

---

## Level 1 — System Overview

At the highest level, TimeForCode consists of four deployable services and a shared data store. The Reporting API is back-end only so far and has no front-end caller yet.

```mermaid
graph LR
    subgraph TimeForCode
        Frontend["Front-end\n(React SPA)"]
        AuthAPI["Authorization API\n(.NET)"]
        DonationAPI["Donation API\n(.NET)"]
        ReportingAPI["Reporting API\n(.NET)"]
        MongoDB[("MongoDB")]
    end

    GitHub["GitHub (External)"]

    Frontend -->|REST calls via BFF| AuthAPI
    Frontend -->|REST calls via BFF| DonationAPI
    AuthAPI -->|OAuth 2.0| GitHub
    AuthAPI -->|persistence| MongoDB
    DonationAPI -->|GitHub REST API| GitHub
    DonationAPI -->|persistence| MongoDB
    ReportingAPI -->|HTTP, caller's token| DonationAPI
```

---

## Level 2 — Authorization Bounded Context

The Authorization context handles all concerns related to user identity, token issuance, and session management.

```mermaid
graph LR
    subgraph Authorization Bounded Context
        AuthAPI_Ctrl["API Layer\n(Controllers)"]
        AuthApp["Application Layer\n(MediatR Handlers)"]
        AuthDomain["Domain Layer\n(OAuthConstants, Models)"]
        AuthInfra["Infrastructure Layer\n(DB, Provider Clients)"]
        AuthValues["Values\n(DTOs, Results)"]
    end

    Client["Front-end / External Client"]
    GitHub["GitHub OAuth"]
    DonationAPI["Donation API
(anonymise contributor)"]
    MongoDB[("MongoDB")]

    Client -->|HTTP requests| AuthAPI_Ctrl
    AuthAPI_Ctrl -->|dispatches commands| AuthApp
    AuthApp -->|reads domain rules| AuthDomain
    AuthApp -->|calls abstractions| AuthInfra
    AuthInfra -->|HTTP| GitHub
    AuthInfra -->|HTTP, service token| DonationAPI
    AuthInfra -->|wire protocol| MongoDB
```

### Components

| Component | Responsibility |
| --- | --- |
| `TimeForCode.Authorization.Api` | REST endpoints: login, callback, refresh, logout, user (profile, repositories, account deletion), JWKS |
| `TimeForCode.Authorization.Application` | MediatR command handlers; token service; account linking |
| `TimeForCode.Authorization.Domain` | OAuth constants and domain primitives |
| `TimeForCode.Authorization.Infrastructure` | GitHub OAuth client; Donation API client (account deletion); MongoDB user and token repositories; RSA key loading |
| `TimeForCode.Authorization.Commands` | MediatR command and result types |
| `TimeForCode.Authorization.Values` | Shared value objects (ExternalAccessToken, TokenResult) |
| `TimeForCode.Authorization.Api.Client` | Typed HTTP client for the Authorization API, generated from its OpenAPI document; used by the specification tests |

---

## Level 2 — Donation Bounded Context

The Donation context manages all concerns related to projects, donations, hour tracking, and organisations.

```mermaid
graph LR
    subgraph Donation Bounded Context
        DonAPI_Ctrl["API Layer\n(Controllers)"]
        DonApp["Application Layer\n(Handlers)"]
        DonDomain["Domain Layer\n(Entities, Factories)"]
        DonInfra["Infrastructure Layer\n(Repositories)"]
    end

    Client["Front-end"]
    GitHub["GitHub REST API"]
    MongoDB[("MongoDB")]

    Client -->|HTTP requests + JWT| DonAPI_Ctrl
    DonAPI_Ctrl -->|commands| DonApp
    DonApp -->|domain logic| DonDomain
    DonApp -->|persistence| DonInfra
    DonInfra -->|HTTP| GitHub
    DonInfra -->|wire protocol| MongoDB
```

### Components

| Component | Responsibility |
| --- | --- |
| `TimeForCode.Donation.Api` | REST endpoints for project registration, search and listing, detail, and the review lifecycle (submit, approve, request changes, archive, re-activate; policies `ApiUser` and `ApiAdmin`); donor-organization creation, listing, detail, update, and deletion; logging and listing project contributions; pledging hours to a project, listing donations and activating or cancelling them (policies `ApiUser` and `ApiUserOrAdmin`) |
| `TimeForCode.Donation.Application` | MediatR command handlers; GitHub metadata fetch interface |
| `TimeForCode.Donation.Domain` | Project entity with lifecycle state machine (Draft, PendingApproval, Active, Archived); GithubSnapshot record; DocumentEntity base class |
| `TimeForCode.Donation.Infrastructure` | MongoDB project repository (including `ProjectSearchDefinitions`: the active-project search filter and the status, language and topic indexes created at startup); GitHub public REST API client |
| `TimeForCode.Donation.Commands` | MediatR command, query, and result types |
| `TimeForCode.Donation.Values` | Shared value objects (ProjectStatus and DonationStatus enums) |
| `TimeForCode.Donation.Api.Client` | Typed HTTP client for the Donation API, generated from its OpenAPI document; used by the specification tests |

### Domain Entities

| Entity | Responsibility |
| --- | --- |
| `Project` | Registered open-source project; linked to a GitHub repository |
| `Donation` | Own MongoDB document: hours pledged to a project by a donor organization or an individual (never both) with the lifecycle Pending, Active, Completed, Cancelled; counts hours logged on its project while Active and completes itself when they reach the commitment |
| `DonorOrganization` | Company registered as a donor |
| `Contributor` | Developer who logs hours; own MongoDB document, upserted by platform user id (unique index) on every logged contribution; also holds the profile (role, normalised skills, hours available, optional `DonorOrganizationId`, indexed), changed only through `UpdateProfile` and the profile endpoints |
| `Contribution` | Time-log entry of a contributor on an `Active` project; stores the `ContributorId` plus a snapshot of the GitHub login and avatar (extended reference, like `GithubSnapshot` on `Project`); the entity enforces the hour and GitHub-reference limits itself and decides who may change it: `Correct` and `MarkDeleted` return a `ContributionChange` (the changed contribution and the hours delta, or the refusal `Deleted`/`NotAllowed`); saves are version-checked (`Version`, a concurrent change is a conflict); a correction keeps the replaced hours and reference in `History` (with corrector and time), a deletion only sets `DeletedAt`/`DeletedByUserId` and reads exclude deleted contributions; `DonationAllocation.PlanRelease` decides which allocations give hours back; `DonationHoursCounter` (shared with logging) counts hours toward Active donations and `DonationHoursReleaser` takes released hours back from the donation allocations (donation saved first, then the allocation shrinks or is removed) |
| `Maintainer` | Developer who owns projects; own MongoDB document, upserted by platform user id (unique index) when a project is registered, with a snapshot of the GitHub login and avatar. Projects link to it through `PublishedByUserId` (indexed); owner-only project operations compare that field with the caller's platform user id, so the maintainer document is not read for ownership checks |
| `Milestone` | Phase within a project, synced from GitHub milestones |

---

## Level 2 — Reporting Bounded Context

The Reporting context turns the donations and contributions of a donor organization into an impact report and a downloadable contribution history. It owns no data: it reads from the Donation context over HTTP with the caller's own access token (ADR-010, [Shared project coupling boundary](../../reference/shared-project-coupling-boundary.md)) and never writes to it. The Donation context decides who may read an organization, and Reporting passes its unauthorized, forbidden and not found answers on.

```mermaid
graph LR
    subgraph Reporting Bounded Context
        RepAPI_Ctrl["API Layer\n(Controller)"]
        RepApp["Application Layer\n(Handlers, export formatter)"]
        RepDomain["Domain Layer\n(ImpactReport)"]
        RepInfra["Infrastructure Layer\n(Donation data source)"]
    end

    Client["Caller + JWT"]
    DonationAPI["Donation API"]

    Client -->|HTTP requests| RepAPI_Ctrl
    RepAPI_Ctrl -->|queries| RepApp
    RepApp -->|report rules| RepDomain
    RepApp -->|IDonationDataSource| RepInfra
    RepInfra -->|generated client, caller's token| DonationAPI
```

### Components

| Component | Responsibility |
| --- | --- |
| `TimeForCode.Reporting.Api` | `ReportingController` with the impact report and the contribution history export (policy `ApiUserOrAdmin`); takes the caller's bearer token from the request and hands it to the query; maps failures to problem details (`401`, `403`, `404`, `502`) |
| `TimeForCode.Reporting.Application` | `GetImpactReportHandler`, `GetContributionHistoryExportHandler`, `ContributionHistoryFormatter` (CSV with RFC 4180 quoting and spreadsheet formula guard, JSON) and the `IDonationDataSource` interface |
| `TimeForCode.Reporting.Domain` | `ImpactReport` and `ProjectImpact` (only `Active` and `Completed` donations count), `OrganizationDonation` and `OrganizationContribution` as read from Donation |
| `TimeForCode.Reporting.Commands` | Queries and results; `Result<T>` (duplicated per context on purpose) |
| `TimeForCode.Reporting.Values` | `DonationStatus` (duplicated from Donation on purpose) and `ExportFormat` |
| `TimeForCode.Reporting.Infrastructure` | `DonationApiDataSource` on the generated `TimeForCode.Donation.Api.Client` with a bearer header per call; `DonationApiOptions` (`BaseUri`); the only place that may reference the Donation client |

Architecture tests (`TimeForCode.Reporting.Architecture.Tests`) enforce the layer rules and the context boundary: Reporting may use nothing of Donation except the generated client, only from Infrastructure, never anything of Authorization, and Donation and Authorization must not know Reporting.

---

## Level 2 — Front-end

The front-end is a React single-page application written in TypeScript and built with Vite (`src/Frontend/TimeForCode.Frontend`). It is the user-facing shell and contains no business logic. It reaches the Authorization API and the Donation API through a same-origin backend-for-frontend (BFF) that turns the `HttpOnly` token cookies into bearer tokens (target state, [ADR-012](../../reference/adr/0012-react-spa-frontend.md)). Stack, folder layout and testing strategy: [front-end architecture](../../frontend/architecture.md).

| Folder | Responsibility |
| --- | --- |
| `src/app` | Application shell, routing and providers |
| `src/components` | Design-system components |
| `src/features` | One folder per feature with its pages, queries and tests |
| `src/messages` | All user-facing strings |
| `src/api` | Clients generated from the OpenAPI snapshots (target state) |
| `tests` | Shared test setup and helpers, including the axe accessibility check |

Component tests run with Vitest, Testing Library and axe. Interactive and informational elements get `data-testid` attributes, so the browser-level Playwright acceptance tests can select them independently of the markup.

---

## Level 2 — Shared

| Component | Responsibility |
| --- | --- |
| `TimeForCode.Shared` | `CookieAuthorizationHandler` reads the JWT cookie and attaches it as a bearer token on outgoing HTTP calls. `AuthorizeFilter` validates the token on incoming requests. |

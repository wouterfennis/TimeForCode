
# Time For Code — Documentation

![Project banner](images/banner.png)

This is the documentation anchor for the TimeForCode platform. All requirements, architecture decisions, implementation references, and product designs live here. AI agents and human contributors should treat this folder as the single source of truth.

> **Navigation**: Start with [Current Platform](#current-platform) to understand what is built, or [Target Platform](#target-platform) to understand what is being built. Architecture is always governed by the [Arc42](#architecture-arc42) documents.

---

## Current Platform

Documents that reflect the system as it exists in code today.

| Document | Purpose |
| --- | --- |
| [Overview](current/overview.md) | Implemented capabilities by bounded context |
| [Capability Status](current/capability-status.md) | Feature-level implementation status (done / partial / missing) |
| [API Surface](current/api-surface.md) | Existing endpoints and contracts |
| [Testing](current/testing.md) | Current test projects and verification strategy |
| [Deployment Status](current/deployment-status.md) | Local docker and Azure Bicep topology |

---

## Target Platform

Documents that define what the platform should become. These are the build instructions for future implementation.

| Document | Purpose |
| --- | --- |
| [Product Vision](target/product-vision.md) | Mission, user segments, and value proposition |
| [Personas and Journeys](target/personas-and-journeys.md) | Key actors and end-to-end user journeys |
| [Requirements](target/requirements.md) | Functional and non-functional requirements |
| [Capability Map](target/capability-map.md) | Target capabilities mapped against current status |
| [Domain Model](target/domain-model.md) | Target domain model and entity relationships |
| [Authorization and Roles](target/authorization-and-roles.md) | Role model and permission boundaries |
| [API Contracts](target/api-contracts.md) | Intended API surface for all domains |
| [Roadmap](target/roadmap.md) | Phased delivery plan |

---

## Architecture (Arc42)

Architecture is documented using the [Arc42](https://arc42.org) template. All architecture-impacting changes must update the relevant Arc42 section. See [docs/architecture/arc42/README.md](architecture/arc42/README.md) for the index and governance rules.

| Section | Title |
| --- | --- |
| [01](architecture/arc42/01-introduction-and-goals.md) | Introduction and Goals |
| [02](architecture/arc42/02-architecture-constraints.md) | Architecture Constraints |
| [03](architecture/arc42/03-context-and-scope.md) | Context and Scope |
| [04](architecture/arc42/04-solution-strategy.md) | Solution Strategy |
| [05](architecture/arc42/05-building-block-view.md) | Building Block View |
| [06](architecture/arc42/06-runtime-view.md) | Runtime View |
| [07](architecture/arc42/07-deployment-view.md) | Deployment View |
| [08](architecture/arc42/08-crosscutting-concepts.md) | Crosscutting Concepts |
| [09](architecture/arc42/09-architecture-decisions.md) | Architecture Decisions |
| [10](architecture/arc42/10-quality-requirements.md) | Quality Requirements |
| [11](architecture/arc42/11-risks-and-technical-debt.md) | Risks and Technical Debt |
| [12](architecture/arc42/12-glossary.md) | Glossary |

---

## Reference

| Document | Purpose |
| --- | --- |
| [Glossary](reference/glossary.md) | Shared domain and technical terms |
| [ADR Index](reference/adr/README.md) | Architecture Decision Records |
| [Docs Conventions](README.md) | Documentation standards, status labels, and authoring rules |

---

## Workflow Documentation

| Document | Purpose |
| --- | --- |
| [Authentication Flow](authentication/authentication_flow_design.md) | OAuth 2.0 login and token lifecycle (implemented) |
| [Donation Workflows](donation/donation-sequence-diagrams.md) | Project onboarding and donation lifecycle |

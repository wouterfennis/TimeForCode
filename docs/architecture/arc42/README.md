# Arc42 Architecture Documentation

This folder contains the architecture documentation for TimeForCode, structured according to the [Arc42 template](https://arc42.org).

Arc42 is the single source of truth for all architecture decisions, structural views, and quality requirements. Any change that impacts system architecture must update the relevant section(s) here.

---

## Sections

| Section | Title | Mermaid Required |
|---|---|---|
| [01](01-introduction-and-goals.md) | Introduction and Goals | |
| [02](02-architecture-constraints.md) | Architecture Constraints | |
| [03](03-context-and-scope.md) | Context and Scope | ✅ |
| [04](04-solution-strategy.md) | Solution Strategy | |
| [05](05-building-block-view.md) | Building Block View | ✅ |
| [06](06-runtime-view.md) | Runtime View | ✅ |
| [07](07-deployment-view.md) | Deployment View | ✅ |
| [08](08-crosscutting-concepts.md) | Crosscutting Concepts | |
| [09](09-architecture-decisions.md) | Architecture Decisions | |
| [10](10-quality-requirements.md) | Quality Requirements | |
| [11](11-risks-and-technical-debt.md) | Risks and Technical Debt | |
| [12](12-glossary.md) | Glossary | |

---

## Governance Rules

1. Any change that affects system structure, technology choices, integration points, or deployment topology must update the relevant Arc42 section before the pull request is merged.
2. Architecture decisions must be recorded as ADRs in [docs/reference/adr/](../../reference/adr/README.md) and referenced from [Section 09](09-architecture-decisions.md).
3. Current vs target architecture statements inside each section must be explicitly labelled. Use `> Current:` and `> Target:` block quotes where both apply.
4. Quality requirements in Section 10 must include measurable acceptance criteria.
5. Sections 3, 5, 6, and 7 must contain at least one Mermaid diagram.

---

## How to Update

When implementing a feature that changes architecture:

1. Identify which section(s) are affected.
2. Update the content and any Mermaid diagrams.
3. If you made a significant architecture decision, add an ADR and link it from Section 09.
4. Update the status label of any related `docs/current/` document if the implementation now matches a target-state description.

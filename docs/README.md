# Documentation Conventions

This file defines the authoring rules for all documentation in the `docs/` folder. Follow these rules when adding or updating any document.

---

## Document Status Labels

Every document should declare its status at the top, directly after the heading:

| Label | Meaning |
|---|---|
| `Status: Current` | Reflects the implemented system as it exists in code today |
| `Status: Target` | Defines intended future behaviour; not yet implemented |
| `Status: Mixed` | Covers both current and target; sections must be clearly labelled |
| `Status: Draft` | Work in progress; not yet complete or reviewed |

Example:

```markdown
# My Document

Status: Current
```

---

## Folder Structure

```
docs/
├── index.md                          # Main navigation hub
├── README.md                         # This file — conventions and rules
├── current/                          # Current implementation references
├── target/                           # Target platform design documents
├── architecture/
│   └── arc42/                        # Arc42 architecture documentation
├── reference/
│   ├── glossary.md                   # Shared terminology
│   └── adr/                          # Architecture Decision Records
├── authentication/                   # Authentication flow documentation
└── donation/                         # Donation workflow documentation
```

---

## Diagram Rules

1. **Mermaid is mandatory** for all architecture and workflow diagrams.
2. Every diagram must have surrounding prose. A diagram alone is not sufficient — explain the intent, inputs, outputs, and any decision points in text.
3. Any section that describes cross-system interactions, runtime flows, or request/response sequences must include at least one Mermaid diagram.
4. Place diagrams immediately next to the text they support; do not create diagram-only pages.
5. Prefer small, focused diagrams. Split complex flows into multiple diagrams rather than producing a single large one.
6. Arc42 sections 3, 5, 6, and 7 must include Mermaid diagrams by default.

Supported Mermaid diagram types for this project:

| Diagram type | Use for |
|---|---|
| `graph` / `flowchart` | Context diagrams, decision flows |
| `sequenceDiagram` | Runtime interactions between services |
| `stateDiagram-v2` | Lifecycle states (e.g. donation states) |
| `C4Context` | System context (Arc42 Section 3) |
| `graph LR` | Deployment topology |

---

## Architecture Changes

Any change that affects system architecture must:

1. Update the relevant Arc42 section(s) in `docs/architecture/arc42/`.
2. Create or update an Architecture Decision Record in `docs/reference/adr/` if a significant decision was made.
3. Reference the ADR from Arc42 Section 09.

---

## Updating Current vs Target Documents

- If you implement a feature that was previously Target, move or update the relevant content to `docs/current/` and change the status label.
- Do not delete Target documents when implementing features; update them to reflect the new state and cross-reference the current implementation.

---

## Language and Style

- Write in plain English. Prefer short sentences and active voice.
- Documents must be useful without rendering diagrams — always provide text that explains what the diagram shows.
- Avoid implementation details such as class names or method signatures in design documents; keep those in code comments or API surface docs.

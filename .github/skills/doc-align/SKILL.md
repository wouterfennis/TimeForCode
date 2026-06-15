---
name: doc-align
description: Reviews all documentation in the TimeForCode repository and aligns it with the current state of the code. Checks sequence diagrams, capability tables, test descriptions, deployment topology, and risk/debt entries against what is actually implemented. Excludes future-plan documents. Use this skill when documentation alignment is requested, after a significant implementation phase, or as part of a release review.
---

# Documentation Alignment Skill

This skill walks every documentation file that describes the **current** state of the system and verifies it matches the code. Follow every step in order.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `maintenance` |
| Status | `active` |
| Overlap risk | `none` |
| Review cadence | `per-release` |

---

## Trigger Conditions

Invoke this skill when:

- Documentation alignment is requested after a significant implementation phase
- The Maintenance agent runs its documentation hygiene pass
- The Release Manager agent's release-readiness check includes documentation currency

---

## Required Inputs

| Input | Source |
|-------|--------|
| Repository root path | Working directory |
| Authenticated `dotnet` SDK (for build verification where needed) | Verified before running any command |

---

## Expected Outputs

- A structured alignment report with three sections: Accurate, Updated, Skipped (Target)
- A list of files edited with one-line descriptions of each change

---

## Scope Rules

### Files to review — must match code exactly

| File | What to verify |
| --- | --- |
| `README.md` | Current Status section; listed capabilities |
| `docs/current/overview.md` | Implemented capabilities per bounded context |
| `docs/current/capability-status.md` | Feature-level status (done / partial / missing) |
| `docs/current/api-surface.md` | Endpoint list, verbs, routes, auth, response codes |
| `docs/current/testing.md` | Test projects listed; mermaid diagram; Known Gaps |
| `docs/current/deployment-status.md` | Container topology diagram; environment table |
| `docs/authentication/authentication_flow_design.md` | OAuth sequence; token storage |
| `docs/donation/donation-sequence-diagrams.md` | Only **Workflow 1** (project registration); all other workflows are target |
| `docs/architecture/arc42/02-architecture-constraints.md` | Architecture constraints |
| `docs/architecture/arc42/09-architecture-decisions.md` | ADR index |
| `docs/architecture/arc42/11-risks-and-technical-debt.md` | Risk and debt entries |
| `docs/architecture/arc42/12-glossary.md` | Term definitions |
| Arc42 **Mixed** sections (01, 03–08) | Only subsections explicitly marked or clearly describing current state |

### Files to skip — target / future plans

- `docs/target/` — all files
- `docs/architecture/arc42/10-quality-requirements.md` (`Status: Target`)
- `docs/donation/donation-sequence-diagrams.md` Workflows 2–5
- Any subsection inside a Mixed arc42 file that is clearly labelled as future/target

---

## Step 1 — Inventory Endpoints

Read every controller file under `src/` and produce a table:

| Bounded Context | HTTP Verb | Route | Auth Policy | Status |
| --- | --- | --- | --- | --- |

```powershell
Get-ChildItem -Recurse -Filter "*Controller.cs" -Path src | Select-String -Pattern "\[Http(Get|Post|Put|Delete|Patch)\]|\[Route\]|\[Authorize" | Select-Object Path, Line
```

Compare this table against `docs/current/api-surface.md`. Flag any endpoint present in code but missing from the doc, and vice versa.

---

## Step 2 — Inventory Test Projects

List every project under `tst/`:

```powershell
Get-ChildItem -Recurse -Filter "*.csproj" -Path tst | Select-Object -ExpandProperty FullName
```

Compare against `docs/current/testing.md`. Verify:

- Every test project listed in the doc exists on disk.
- Every test project on disk appears in the doc.
- The Mermaid diagram in `testing.md` shows the correct set of test projects and their relationships.
- The **Known Gaps** section accurately reflects what is absent (e.g., no architecture tests for Donation context).

---

## Step 3 — Check Deployment Topology

Read `docker-compose.yaml` and list every service name and its exposed ports. Compare against the diagram and environment table in `docs/current/deployment-status.md`.

Key things to verify:

- Container names match.
- Port mappings match.
- Environment variable overrides (e.g., `GithubApiOptions__BaseUrl`) are documented.
- `docker-compose.real-github.yml` overlay is mentioned.
- Commands use `podman compose`, not `docker compose` or `docker-compose`.

---

## Step 4 — Check Sequence Diagrams (Workflow 1 Only)

Read `docs/donation/donation-sequence-diagrams.md` **Workflow 1** only.

Cross-check against `src/Donation/TimeForCode.Donation.Api/V1/Controllers/ProjectController.cs` and the corresponding handler under `src/Donation/TimeForCode.Donation.Application/Handlers/`.

Verify:

- Auth policy shown in the diagram matches the `[Authorize(Policy = "...")]` attribute in code.
- Project status after registration matches what the handler actually sets (currently `Published`).
- No steps appear in the diagram that do not exist in the handler (e.g., admin approval, notifications).
- The status note at the top of the workflow says "Fully implemented" if the handler is complete.

---

## Step 5 — Check Capability Status

Read `docs/current/capability-status.md`. For each row marked **Done** or **Partial**, verify the corresponding code exists. For each row marked **Missing**, verify the code is genuinely absent.

Key bounded contexts:

- **Authorization**: `src/Authorization/`
- **Donation**: `src/Donation/`
- **Website**: `src/Website/`

---

## Step 6 — Check Risks and Technical Debt

Read `docs/architecture/arc42/11-risks-and-technical-debt.md`.

For each DEBT entry:

- If the entry says a layer is "empty" or "not implemented", verify that is still true by checking the corresponding `src/` folder.
- If implementation exists, update the entry to reflect what was done and narrow the remaining gap.

---

## Step 7 — Check Crosscutting Concepts Accuracy

Read `docs/architecture/arc42/08-crosscutting-concepts.md`.

Verify any claims that say "each bounded context" does something (e.g., has architecture tests, uses a specific pattern) are actually true for **all** bounded contexts. If a context is an exception, the doc must say so explicitly.

---

## Step 8 — Report Findings

Produce a structured report with three sections:

### Accurate

List each file reviewed and confirmed accurate.

### Updated

List each file edited, with a one-line description of what changed.

### Skipped (Target)

List files skipped because they describe future plans.

---

## Key Code-to-Doc Mappings (Quick Reference)

| Code location | Primary doc |
| --- | --- |
| `src/Donation/TimeForCode.Donation.Api/V1/Controllers/ProjectController.cs` | `docs/current/api-surface.md`, `docs/donation/donation-sequence-diagrams.md` (Wf1) |
| `src/Authorization/TimeForCode.Authorization.Api/V1/Controllers/` | `docs/current/api-surface.md` |
| `tst/` (all test projects) | `docs/current/testing.md` |
| `docker-compose.yaml` | `docs/current/deployment-status.md` |
| `deploy/*.bicep` | `docs/current/deployment-status.md` |
| `src/Donation/TimeForCode.Donation.Application/Handlers/` | `docs/donation/donation-sequence-diagrams.md` (Wf1) |
| `tst/Authorization/TimeForCode.Authorization.Architecture.Tests/` | `docs/architecture/arc42/08-crosscutting-concepts.md` |

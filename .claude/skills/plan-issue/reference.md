# Plan reference

Detail for `/plan-issue`. Read the section you need.

## Step 2 — Exploration checklist (in this order)

1. **arc42** (`docs/architecture/arc42/`): `05-building-block-view.md` (components), `06-runtime-view.md` (flows), `08-crosscutting-concepts.md` (auth, errors, logging), `09-architecture-decisions.md` (prior decisions), `02-architecture-constraints.md` (hard limits), `10-quality-requirements.md` (goals not to regress).
2. **Current state** (`docs/current/`): `api-surface.md` (do not duplicate endpoints), `capability-status.md` (implemented vs planned), `testing.md`.
3. **Source** in `src/` for the affected area: which interfaces, handlers, entities exist.
4. **Tests** in `tst/`: current coverage and what must not break.

If arc42 disagrees with the code, note it under Additional Context. Only reference things verified to exist. For broad searches use the `repo-scout` subagent.

## Step 3 — When to split (at least two must be true)

- [ ] Acceptance criteria describe two or more independently implementable, specifiable and reviewable pieces.
- [ ] Affected areas span more than one module boundary in ways that do not share one TDD pass.
- [ ] Some criteria are user-observable (need Gherkin) while others are purely internal (refactor, infra, debt).
- [ ] The problem statement itself describes multiple capabilities, phases or milestones.

When splitting: define the parent scope (overall outcome, complete only when every child is done) and each child scope (independently shippable, small enough for the normal flow). Label each child with exactly one of:

- `agent-phase:feature-writer` — has a user-observable criterion (run `/write-feature`).
- `agent-phase:implementation-only` — every criterion is internal (skip `/write-feature`).

Add `agent-phase:skip-markdown-lint` when the child touches no `.md` files or documentation. Not splitting: no phase labels; the default sequence applies.

State the decision in one or two sentences before drafting, for example: "This splits into 2 pieces: a Donation API change (needs a spec) and a Website display change (needs a spec). Both ship independently. I'll draft one parent and two children."

## Step 4 — Draft rules

Structure follows `.github/ISSUE_TEMPLATE/planned-work.yml`; templates are in skill `issue-draft-templates`. Title `[Type]: Short, actionable description`. Motivation explains the why without assuming context. Affected Areas lists only verified files. Acceptance Criteria are specific, measurable, independently testable, and written so each can become a failing test first (TDD). No code or pseudocode. The parent has a `## Child Issues` task list (`- [ ] #<N>`) filled after the children exist.

## Step 5 — Self-verification checklist

| # | Check | Pass condition |
| --- | --- | --- |
| 1 | Title | `[Type]: Description`, actionable, no jargon |
| 2 | Motivation | States problem or value without assumed background |
| 3 | Affected Areas | Every item verified in the codebase |
| 4 | Acceptance Criteria | Specific, measurable, testable in isolation |
| 5 | No code | No code, pseudocode or signatures |
| 6 | Completeness | A newcomer could start the work |
| 7 | Labels | Plausible; validated against `gh label list` at submission |

For a parent/child set also:

| # | Check | Pass condition |
| --- | --- | --- |
| 8 | Independence | Each child stands alone, or the dependency is stated in its Additional Context |
| 9 | Coverage | Every Step 1 criterion belongs to exactly one child |
| 10 | Phase labels | Each child has exactly one of the two phase labels, correctly |
| 11 | Parent scope | Parent holds no criteria that belong to a child |

## Step 6 — Presenting

Show the parent first, then each child. Ask: does this capture what you want to track; (if split) does the split make sense; any changes before submission. A change that affects the split returns to Step 3; always rerun Step 5. Never submit before explicit approval of every draft.

## Step 7 — Submission

Use skill `gh-issue` (mode `create`). Missing `agent-phase:*` label: ask permission before `gh label create`. Single issue: one create. Split: create every child first, then the parent with real child numbers, then add `Part of #<parent>` comments on each child; prefer native sub-issues when available. Report every URL (parent first) and tell the user to comment `APPROVED: plan` on each implementable issue (parent excluded) once reviewed; `/write-feature` requires it.

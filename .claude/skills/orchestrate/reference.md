# Orchestrator reference

## Running Independent Children in Parallel

If two or more children are simultaneously eligible for the same phase (for example, both cleared Phase 2 approval and are ready for Phase 3), and that phase is **Review** (or Markdown Lint), you may launch one `review` subagent per child in the same turn with the Agent tool. Plan, FeatureWriter and Implementation are interactive skills the user runs, so they are never dispatched in parallel. Only do this when:

- Every child being dispatched together has independently satisfied its own gate condition for that phase
- None of the children being dispatched together are noted as depending on one another (check each child's Additional Context section for a stated dependency before batching)

Report the outcome of each dispatched run separately in your next Status Summary — do not merge their results into one line.

---

## State Summary Format

After every check, output a compact status table so the user always knows where they stand. Adapt it to the current shape:

**Single-issue mode:**

```
Issue #N — <title>

| Phase           | Status |
|-----------------|--------|
| Plan (creation) | ✅ Complete |
| FeatureWriter   | ⏳ Awaiting approval on GitHub |
| Implementation  | ⬜ Not started |
| Review          | ⬜ Not started |
| Markdown Lint   | ⬜ Not started |
```

**Parent/child mode:**

```
Parent #N — <title> (epic)

| Child | Title | FeatureWriter | Implementation | Review |
|-------|-------|----------------|-----------------|--------|
| #101  | <child 1 title> | ✅ Approved | ⏳ Awaiting log | ⬜ Not started |
| #102  | <child 2 title> | ➖ N/A (implementation-only) | ⬜ Not started | ⬜ Not started |

Markdown Lint (repo-wide, on parent #N): ⬜ Not started — blocked until all children clear Review
```

Use ✅ for complete, ⏳ for in progress or awaiting action, ⬜ for not yet started, ➖ for a phase that does not apply to that child.

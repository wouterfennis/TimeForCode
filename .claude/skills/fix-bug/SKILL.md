---
name: fix-bug
description: "Bug-fix path (skips Plan/FeatureWriter): reproduce with a failing regression test, fix minimally, verify, log on the issue. Use for defects with a clear, small scope."
argument-hint: "<issue number or bug description>"
disable-model-invocation: true
---

# Fix Bug (TDD)

Use for a defect that does not need a new feature file. If the fix changes behaviour that acceptance criteria describe, or touches several modules, stop and use the full flow (`/plan-issue`).

1. **Understand**: read the issue (`gh-compact-view`) or the description. Locate the code with Grep or the `repo-scout` subagent. For unclear causes use `/root-cause-analysis` first.
2. **Red**: write the smallest test that reproduces the bug at the lowest sensible level (unit before Specification). Run it (skill `quiet-dotnet`, scoped project, `--filter`) and confirm it fails because of the bug, not setup. Rules: `.claude/rules/testing-strategy.md`.
3. **Green**: minimal fix. Do not refactor unrelated code. Never alter the failing test to pass.
4. **Refactor** only with the suite green; rerun the project's tests.
5. **Verify**: one full `dotnet build TimeForCode.sln` and `dotnet test TimeForCode.sln --no-build`. A `SwaggerTests` diff follows `swagger-snapshot-review`.
6. **Docs**: update `docs/current/` or arc42 chapter 11 if the bug exposed a known limitation.
7. **Log**: post an `## Implementation Run Log` (`gh-issue` mode `impl-log`) listing the regression test, the cause, the fix and loose ends. Then run the `review` agent and `/ship-pr`.

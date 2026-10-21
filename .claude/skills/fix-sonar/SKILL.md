---
name: fix-sonar
description: "Triage open SonarCloud findings via the web API and fix one rule or file group per run, test-first where behaviour could change. Use for code smells, bugs, vulnerabilities and security hotspots reported by Sonar."
argument-hint: "[rule key | file path | 'hotspots' | 'gate']"
disable-model-invocation: true
---

# Fix Sonar findings

Sonar is a quality gate, not a feature source: small, mechanical, reviewable changes. Never touch behaviour without a test. Never bulk-fix across modules in one PR.

Data comes from `scripts/sonar-issues.ps1` (read-only SonarCloud API, compact output). Do not open the Sonar website or fetch raw API JSON. Auth is `SONAR_TOKEN` in the environment; never read, print or commit it. If a call returns 401/403, tell the user to set it and stop.

## Steps

1. **Read** `.claude/lessons-learned.md` (grep for Sonar or the rule key).
2. **Branch**: work on a `sonar/<rule-or-area>` branch off `main`, never on `main`. Sonar analyses `main`, so compare against that.
3. **Triage**: `pwsh scripts/sonar-issues.ps1 -Mode summary` (add `-Mode gate` first if a failing gate is the trigger). Pick **one** target, in this order unless the user named one: security hotspots and vulnerabilities, bugs, then code smells by severity. Within that prefer the rule with the most hits in one module. State the target and the count; ask only if the choice is ambiguous.
4. **Inspect**: `-Mode list -Rule <key> -Limit 50` for locations, `-Mode rule -Rule <key>` for the rule's rationale. Read only the affected line ranges.
5. **Classify each finding**:
   - *Mechanical, behaviour-preserving* (unused usings, naming, redundant code): fix directly; existing tests are the safety net.
   - *Behaviour could change* (null handling, exception paths, logic, security): red test first, then minimal fix (TDD rules in `.claude/rules/testing-strategy.md`).
   - *False positive or accepted risk*: do **not** suppress in code or silently skip. List it with a one-line justification for the user, who decides on a Sonar status change or a documented `TODO(review)` / arc42 chapter 11 entry. Hotspots are only ever reviewed by the user.
6. **Verify**: `dotnet build TimeForCode.sln`, then one `dotnet test TimeForCode.sln --no-build` run (skill `quiet-dotnet`). Architecture tests must stay green. A `SwaggerTests` diff follows `swagger-snapshot-review`.
7. **Format**: `pwsh -NoProfile -File .claude/hooks/format-solution.ps1` (see `lessons-learned` about unrelated churn; revert files you did not touch).
8. **Report and ship**: summarise per rule: fixed N, skipped M (with reasons), tests added. Then the `review` agent and `/ship-pr`. The PR body links the rule keys; do not claim the gate passes, because Sonar only re-analyses in CI. After CI, re-run `-Mode gate -PullRequest <n>` and `-Mode list -PullRequest <n>` to confirm no new findings.

## Stop conditions

- More than ~40 findings or more than 2 modules in the target: split it and fix the first slice only.
- A fix needs a public API, data shape or architecture change: stop and propose an issue via `/plan-issue`.
- Findings under `src/Website/` (deprecated, not in the solution or CI, `src/Website/DEPRECATED.md`): skip and report them; suggest excluding the path in the Sonar scan instead.
- Coverage or duplication findings: report them, do not chase them here.

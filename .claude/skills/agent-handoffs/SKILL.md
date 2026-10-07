---
name: agent-handoffs
description: "Artifacts, quality gates and stop conditions at each Plan, FeatureWriter, Implementation and Review phase boundary. Use when starting or finishing a workflow phase."
---

# Agent Handoff Instructions

This file defines the minimum artifacts, quality gates, and pass/fail conditions that must be satisfied at every phase boundary in the TimeForCode agent workflow.

---

## Workflow Phases

```text
Plan → FeatureWriter → Implementation → Review
```

Each phase produces a mandatory output artifact. The next phase **must not start** until the previous phase has produced its artifact and that artifact has received explicit human approval on the GitHub issue.

---

## Phase 1 — Plan → FeatureWriter

### Output artifact

A GitHub Issue that contains:

- `[Type]: Short description` title
- A complete **Acceptance Criteria** section with at least one checkbox item
- A non-empty **Proposed Solution** section
- A non-empty **Affected Areas** section referencing verified file paths

### Quality gates

| Gate | Pass condition |
| --- | --- |
| Issue exists | `gh issue view <N>` returns a 200 response |
| Title format | Title matches `^\[.+\]: .+` |
| Acceptance criteria | At least one `- [ ]` checkbox is present |
| No code | Issue body contains zero code fences with implementation code |

### Stop condition

If the issue is missing any required section, the FeatureWriter agent must stop and post a comment asking the user to complete the issue before proceeding.

---

## Phase 2 — FeatureWriter → Implementation

### Output artifact

A Gherkin feature file posted as a comment on the originating issue. The comment must contain a fenced `gherkin` block with at least one `Scenario` or `Scenario Outline`.

### Quality gates

| Gate | Pass condition |
| --- | --- |
| Feature file comment exists | A comment by the FeatureWriter on the issue contains ` ```gherkin ` |
| At least one scenario | The Gherkin block contains `Scenario:` or `Scenario Outline:` |
| Scenarios map to AC | Every acceptance-criteria checkbox has at least one covering scenario |
| Persona convention | Every `Given`/`When`/`Then` step uses "The user", "The external platform", or "The time for code platform" as the subject |
| No implementation detail | Step text contains no HTTP verbs, status codes, class names, or method signatures |

### Stop condition

If the feature file is absent or any gate fails, the Implementation agent must stop and request a valid feature file before writing any code.

---

## Phase 3 — Implementation → Review

### Output artifact

An implementation log comment on the originating issue (heading `## Implementation Run Log`) that contains:

- **Completed** — list of files created or modified
- **Loose Ends** — known gaps or partial implementations
- **Open Questions** — unresolved design or requirement questions

### Quality gates

| Gate | Pass condition |
| --- | --- |
| Test-first evidence | Each new behaviour had a red test run before its production code (see `.claude/rules/testing-strategy.md`); the log states which tests were seen red |
| Build passes | `dotnet build TimeForCode.sln` exits 0 (final, once) |
| Tests pass | `dotnet test TimeForCode.sln --no-build` exits 0 with 0 failures (final, once; scoped runs while iterating) |
| No `TODO(review):` left unacknowledged | Every `TODO(review):` marker appears in the Loose Ends section |
| Implementation log posted | Issue has a comment with heading `## Implementation Run Log` |
| Feature file satisfied | Every scenario in the Gherkin block has a corresponding Reqnroll step definition |

### Stop condition

If the build or tests fail, the Implementation agent must fix the failure before posting the implementation log. If the failure cannot be resolved, it must be listed under Loose Ends with a clear description.

---

## Phase 4 — Review

### Output artifact

A review report comment on the originating issue (heading `## Code Review Report`) that covers:

- Build and test status
- Acceptance criteria coverage table
- Code findings table (🔴 Critical / 🟡 Warning / 🔵 Info)
- Unresolved `TODO(review):` markers
- Documentation gaps

### Quality gates

| Gate | Pass condition |
| --- | --- |
| Review report posted | Issue has a comment with heading `## Code Review Report` |
| All AC assessed | Every acceptance criterion has a ✅, ⚠️, or ❌ status |
| Critical findings addressed | No 🔴 Critical finding is left without a recommended next step |

### Stop condition

The Review agent may not mark a phase complete if any 🔴 Critical finding is present and no remediation path is described.

---

## Human Approval Gates

Human approval is required at these two points before the next phase begins:

1. **After Plan** — the user must explicitly approve the GitHub Issue before FeatureWriter starts.
2. **After FeatureWriter** — the user must explicitly approve the Gherkin feature file before Implementation starts.

**Approval marker.** An approval is a comment by a human (not the agent) on the issue whose first line is exactly `APPROVED: plan` or `APPROVED: feature`. Free-text such as "looks good" or "not approved" never counts. Each phase skill checks for its marker before starting, and stops with the instruction "post `APPROVED: <phase>` on the issue" if it is missing:

```powershell
gh issue view <number> --json comments --jq '[.comments[] | select(.body | test("^APPROVED: (plan|feature)\\s*$"; "m")) | {a: .author.login, h: (.body | split("\n")[0])}]'
```

There is no orchestrator; the phases are run manually in order: `/plan-issue` → `/write-feature <n>` → `/implement-issue <n>` → `review` agent → `markdown-linter` agent → `/ship-pr <n>`.

---

## Overlap Boundary: MarkdownLinter Agent vs. markdown-lint Skill

| Aspect | MarkdownLinter Agent | markdown-lint Skill |
| --- | --- | --- |
| **Owner** | Autonomous agent — runs full lint cycle end-to-end | Reusable subroutine invoked **by** the MarkdownLinter agent |
| **Trigger** | User explicitly requests a Markdown lint pass, or the workflow reaches its lint step | Called internally by the MarkdownLinter agent only |
| **Scope** | Decides which files to scan, interprets results, decides what to fix | Executes `markdownlint` commands and reports raw output |
| **Decision authority** | Decides whether a violation is safe to auto-fix or requires human input | Executes only — makes no decisions |
| **Output** | Human-readable summary posted to the user | Structured command output returned to the calling agent |
| **Direct invocation by other agents** | No — other agents must not invoke MarkdownLinter mid-workflow | No — other agents must not call the markdown-lint skill directly |

**Rule:** The `markdown-lint` skill is a bounded execution unit. The `MarkdownLinter` agent is the sole orchestrator of that skill. No other agent should invoke either directly, except `/run-maintenance`.

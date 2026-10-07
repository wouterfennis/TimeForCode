# AI setup review (`.claude/`)

Review of the Claude Code configuration: `CLAUDE.md`, `.claude/settings.json`, hooks, agents, rules, commands and skills.
Date: 2026-10-07. Status column: **Done** = fixed in the same change, **Deferred** = decision recorded, not changed.

## Bugs and contradictions

| # | Finding | Status |
| --- | --- | --- |
| 1 | `rules/testing-strategy.md` named xUnit and NetArchTest and listed wrong projects (`*.AcceptanceTests`). Repo uses MSTest, ArchUnitNET, Verify and Reqnroll (`*.Specifications`). | Done: rewritten, now TDD-first |
| 2 | `.agent-state/` (written by the pre-compact hook) was not gitignored. | Done |
| 3 | Tool constraints were prose only; `review` was "read-only" but had unrestricted `Bash`. | Done: `allowed-tools` on helper skills; Bash allowlist hook on restricted agents |
| 4 | `orchestrate` ran on haiku and treated any "approved" substring as a gate pass. | Done: orchestrator removed; gates are now a human-posted `APPROVED:` comment (see `agent-handoffs`) |
| 5 | Conflicting test policy (project-scoped vs `dotnet test TimeForCode.sln` / `--no-incremental` everywhere). | Done: one policy, see `quiet-dotnet` |

## Hooks

| # | Finding | Status |
| --- | --- | --- |
| 6 | No automatic formatting; relied on the agent remembering. | Done: `PostToolUse` hook runs `dotnet format whitespace --folder --include <file>` (~2s) on edited `.cs` files; the solution-wide `dotnet format` stays a pre-commit step |
| 7 | Secret-like files not denied. | Done: `permissions.deny` extended |
| 8 | Deny list only covered a few paths; hook fails open. | Done: deny list extended (hook stays fail-open by design, documented) |
| 9 | Thin allowlist; `Bash(podman:*)` too broad. | Done |
| 10 | Guard matcher skipped the `PowerShell` tool; glob patterns beginning with `!` (exclusions) were wrongly blocked. | Done |
| 11 | No `Stop` hook. | Done: warns when `.cs` changed but no build/test ran |
| 12 | `pwsh` is a hard requirement but undocumented. | Done: in `CLAUDE.md` |
| 13 | Session-start dumped noisy status. | Done: capped, deletions summarised |

## Agents

| # | Finding | Status |
| --- | --- | --- |
| 14 | Only two subagents; phase work bloats the main context. | Done: added `repo-scout`, `test-runner`, `security-reviewer`. Phase skills stay inline because they need `AskUserQuestion`; forking them is **Deferred** |
| 15 | `review` duplicated skill content and had no `skills:` preload. | Done |
| 16 | `markdown-linter` restriction unenforced. | Done: `restrict-bash.ps1` allowlist hook in agent frontmatter (also used by `review`, `test-runner`, `security-reviewer`); it fails open on internal errors; `Edit` is not path-restricted (prompt only) |
| 17 | Dead reference section in `review`. | Done: removed |

## Skills and commands

| # | Finding | Status |
| --- | --- | --- |
| 18 | Four legacy `commands/*.md` next to skills. | Done: migrated to skills, `commands/` removed |
| 19 | Side-effecting skills could be auto-invoked. | Done where safe: user-run skills (`ship-pr`, `fix-bug`, migrated prompts, `issue-triage`) are `disable-model-invocation`. `gh-*`, `changelog-update`, `dependency-update`, `dotnet-format` stay model-invocable because phase skills call them; they carry scoped `allowed-tools` (which pre-approves, it does not restrict) |
| 20 | Large skills load fully. | Done for `implement-issue`; others measured and **Deferred** (see below) |
| 21 | Overlapping skills (`gh-*`, dotnet running, release/maintenance). | Partly done: dotnet running has one source (`quiet-dotnet`). Merging `gh-*` skills is **Deferred** |
| 22 | No PR/commit skill, no bug-fix path, no Mongo review. | Done: `ship-pr`, `fix-bug`, `mongo-review` added |
| 23 | `agent-handoffs` not tied to phases. | Done: phase skills reference it |

## CLAUDE.md and repo

| # | Finding | Status |
| --- | --- | --- |
| 24 | Missing shell/pwsh note, `.agent-state`, test prerequisites. | Done |
| 25 | Pending `.github/` deletions and untracked `.claude/` not committed. | **Open**: user to commit as one change |
| 26 | `settings.local.json` not ignored. | Done |
| 27 | No path rules for docs, deploy, Domain. | Done |

## Extra fixes found while working

- Copilot-era tool names (`create_file`, `run_in_terminal`) in `gh-issue-create` / `gh-issue-comment` replaced; comment/issue body files now go to `$env:TEMP` instead of the repo root.
- `gh-issue-comment` template had a broken nested code fence; fixed with a four-backtick outer fence.
- `write-feature` contradicted itself (no files vs. may write `.feature` files); now comment-only.
- All Markdown under `.claude/`, `CLAUDE.md` and this file passes `markdownlint` (table separator style, blank lines, fence languages, heading levels).

## Remaining ideas (not done)

- Split `plan-issue`, `write-feature`, `dll-api-explorer` into `SKILL.md` + `reference.md` like `implement-issue`.
- Merge `gh-issue-create`, `gh-issue-comment`, `gh-implementation-log` into one `gh-issue` skill.
- Fork `write-feature` / `implement-issue` (`context: fork`) once they no longer need interactive questions.
- Hook `restrict-bash.ps1` and `pre-tool-use.ps1` fail open on internal errors; make restrict-bash fail closed if that proves too loose.
- Re-introduce an orchestrator (removed on request) if gate tracking is missed; keep it sonnet-based with explicit `APPROVED:` markers.

## Verification performed

- `restrict-bash.ps1`: allows a matching command, denies `rm` and a chained `curl` (tested by piping events).
- `pre-tool-use.ps1`: an exclusion glob starting with `!` now passes; a real dependency-folder read is still denied.
- `format-cs.ps1`: removed injected blank lines from a `.cs` file in about 2s; tree left clean.
- Not run: `stop-check.ps1` with real C# changes, subagent frontmatter hooks inside a live subagent, a full `dotnet test` (no C# was changed).

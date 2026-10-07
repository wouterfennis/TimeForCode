# TimeForCode

.NET 10 solution `TimeForCode.sln`; bounded contexts under `src/` and `tst/`: **Authorization**, **Donation**, **Shared**, **Website**. Deploy IaC in `deploy/`, docs in `docs/` (arc42 in `docs/architecture/arc42/`, live state in `docs/current/`).

| Layer (per module) | Path |
|--------------------|------|
| API / Commands / Application / Domain / Infrastructure / Values | `src/<Module>/TimeForCode.<Module>.<Layer>/` |
| Specs (Reqnroll) / Unit (`Api.Tests`, `Application.Tests`) / Infra / Architecture tests | `tst/<Module>/TimeForCode.<Module>.<Kind>/` |

Stack: MediatR + `Result<T>`, MongoDB, MSTest + FluentAssertions + Moq, Reqnroll, ArchUnitNET layer rules (violations break the build). Style comes from `.editorconfig`; run `dotnet format ./TimeForCode.sln` before committing C#.

Commands: `dotnet build TimeForCode.sln` · `dotnet test tst/<Module>/<Project>` (one project until the final check) · `gh issue view <n> --json ... --jq ...` (see skill `gh-compact-view`).

## Token efficiency

- Search first (Grep/Glob), then read only the needed line range. Never read whole large files to look around.
- Never read or list `node_modules`, `obj`, `.git`, lockfiles, binaries or `docs/images` (a hook blocks it).
- Do not re-read a file you just edited or re-fetch an issue already in context.
- Run a lone `dotnet build|test` with no pipes or `&&`: a hook trims the output and prints the full-log path on failure (skill `quiet-dotnet`).
- Fetch GitHub issues with `--jq` projections, never raw `--json ...comments` (skill `gh-compact-view`).
- Reply tersely; no restating plans or diffs.
- On long tasks write progress to `.agent-state/handoff.md` (skill `context-handoff`).

## Workflow and tooling (all in `.claude/`)

Feature workflow: Plan → FeatureWriter → Implementation → Review → Markdown Lint, gated by human approval comments on the GitHub issue.

- `/orchestrate` tracks the gates and tells you the next step. Phases: `/plan-issue`, `/write-feature <n>`, `/implement-issue <n>`, then the `review` and `markdown-linter` subagents.
- Other commands: `/run-maintenance`, `/prepare-release <version> <branch>`, `/acceptance-criteria`, `/issue-refinement`, `/pr-review-focus`, `/root-cause-analysis`.
- Skills (`.claude/skills/`): reference and helpers such as `dotnet-conventions`, `agent-handoffs`, `repo-map`, `dotnet-format`, `markdown-lint`. Rules in `.claude/rules/` load by path (`code-style` for `*.cs`, `testing-strategy` for `tst/**`).
- Hooks (`.claude/settings.json`, scripts in `.claude/hooks/`): read guard, dotnet output trimming, session-start context, pre-compact snapshot.
- `.github/` holds only CI workflows and the issue template.

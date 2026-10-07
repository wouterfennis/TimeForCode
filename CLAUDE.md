# TimeForCode

.NET 10 solution `TimeForCode.sln`; bounded contexts under `src/` and `tst/`: **Authorization**, **Donation**, **Shared**, **Website**. Deploy IaC in `deploy/`, docs in `docs/` (arc42 in `docs/architecture/arc42/`, live state in `docs/current/`).

| Layer (per module) | Path |
| --- | --- |
| API / Commands / Application / Domain / Infrastructure / Values | `src/<Module>/TimeForCode.<Module>.<Layer>/` |
| Specs (Reqnroll) / Unit (`Api.Tests`, `Application.Tests`) / Infra / Architecture tests | `tst/<Module>/TimeForCode.<Module>.<Kind>/` |

Stack: MediatR + `Result<T>`, MongoDB, MSTest + FluentAssertions + Moq, Reqnroll, ArchUnitNET layer rules (violations break the build). Style comes from `.editorconfig`; a hook formats edited `.cs` files, and `dotnet format ./TimeForCode.sln` runs before committing C#.

**Development is test-driven**: red test first (seen failing for the right reason), minimal code to green, then refactor. No production code without a failing test. Details: `.claude/rules/testing-strategy.md`.

Commands: `dotnet build TimeForCode.sln` · `dotnet test tst/<Module>/<Project>` (one project while iterating, the whole solution once at the end) · `gh issue view <n> --json ... --jq ...` (skill `gh-compact-view`).

Tests do not need a running database or container (mocks and in-memory fakes); `podman compose` is only for running the services locally (`scripts/start-local.ps1`).

## Environment

- Windows with PowerShell 7 (`pwsh`) is required: all hooks are `pwsh` scripts and silently do nothing without it. The Bash tool is also available; use POSIX syntax there and PowerShell syntax in the PowerShell tool.
- `.agent-state/` holds local session notes (`handoff.md`, `last-dotnet-run`); it is gitignored. Never commit it.
- Secrets live in gitignored env files (for example `.env.real-github`); never read, print or commit them.

## Token efficiency

- Search first (Grep/Glob), then read only the needed line range. Never read whole large files to look around. For broad searches use the `repo-scout` subagent.
- Never read or list `node_modules`, `obj`, `bin`, `.git`, lockfiles, binaries or `docs/images` (a hook and deny rules block it).
- Do not re-read a file you just edited or re-fetch an issue already in context.
- Run a lone `dotnet build|test` with no pipes or `&&`: a hook trims the output and prints the full-log path on failure (skill `quiet-dotnet`). Use the `test-runner` subagent for slow runs.
- Fetch GitHub issues with `--jq` projections, never raw `--json ...comments` (skill `gh-compact-view`).
- Reply tersely; no restating plans or diffs.
- On long tasks write progress to `.agent-state/handoff.md` (skill `context-handoff`).

## Workflow and tooling (all in `.claude/`)

Feature workflow, run manually in order and gated by human `APPROVED: plan` / `APPROVED: feature` comments on the GitHub issue (skill `agent-handoffs`):

`/plan-issue` → `/write-feature <n>` → `/implement-issue <n>` (TDD) → `review` agent → `markdown-linter` agent → `/ship-pr <n>`.

Small defects: `/fix-bug <n>` (failing regression test first, then fix).

- Other commands: `/run-maintenance`, `/prepare-release <version> <branch>`, `/acceptance-criteria`, `/issue-refinement`, `/pr-review-focus`, `/root-cause-analysis`.
- Subagents (`.claude/agents/`): `review`, `markdown-linter`, `repo-scout`, `test-runner`, `security-reviewer`. Bash for the restricted ones is allowlisted by `hooks/restrict-bash.ps1`.
- Skills (`.claude/skills/`): reference and helpers such as `dotnet-conventions`, `agent-handoffs`, `repo-map`, `quiet-dotnet`, `dotnet-format`, `mongo-review`, `markdown-lint`. Rules in `.claude/rules/` load by path (`code-style` for `*.cs`, `testing-strategy` for `tst/**`, `domain-layer`, `docs`, `deploy`).
- Hooks (`.claude/settings.json`, scripts in `.claude/hooks/`): read guard, dotnet output trimming, C# auto-format after edits, session-start context, pre-compact snapshot, stop-time warning for unverified C# changes.
- `.github/` holds only CI workflows and the issue template.
- Review of this setup and open ideas: `docs/ai-setup-review.md`.

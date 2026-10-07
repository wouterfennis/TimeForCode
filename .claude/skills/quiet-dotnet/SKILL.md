---
name: quiet-dotnet
description: "Token-cheap dotnet build, test and format invocations for TimeForCode. Use when: building, running tests, or checking test failures."
---

# quiet-dotnet

Raw `dotnet build`/`dotnet test` output is thousands of lines. Keep it out of context.

## Rules

1. **One policy for the whole repo**: while iterating (TDD red/green/refactor) scope to one project: `dotnet test tst/<Module>/TimeForCode.<Module>.<Kind>` (add `--filter "FullyQualifiedName~<Name>"`). Run `dotnet build TimeForCode.sln` + `dotnet test TimeForCode.sln --no-build` once at the end (implementation final check, `review`, `/ship-pr`). Release checks (`release-readiness`, `prepare-release`) may add `--no-incremental` for a clean build. For long or noisy runs delegate to the `test-runner` subagent.
2. **Skip redundant work**: `--no-restore` after a restore, `--no-build` right after a build. Do not use `--no-incremental` unless a stale build is suspected.
3. **Use the plain command.** A `PreToolUse` hook rewrites a lone `dotnet build|test ...` (optionally ending in `2>&1`) to `.claude/hooks/quiet-run.ps1`, which prints only errors/warnings/summary (max 60 lines) and the full-log path on failure. Do not add pipes, `;` or `&&` to it — a compound command bypasses the trimming.
4. **Do not pass `--logger "console;verbosity=normal"`** — it defeats the filter. Use `--filter "FullyQualifiedName~<Name>"` to rerun only failing tests.
5. **On failure**, fix from the trimmed output first. Open the full log (path printed as `full output: ...`) only with a ranged read around the failing test name.
6. Without hooks (other tooling), emulate: `dotnet test <proj> --nologo -v q 2>&1 | Select-String -Pattern 'error|Failed|Passed!|Total' | Select-Object -First 40`.

## Format

`dotnet format ./TimeForCode.sln` (see skill `dotnet-format`); verify with one scoped build afterwards, not a full `--no-incremental` build.

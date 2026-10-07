---
name: ship-pr
description: "Final workflow step: verify gates, commit with the attribution trailer, push the branch and open a pull request that links the issue. Asks before pushing."
argument-hint: "<issue number>"
disable-model-invocation: true
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git log:*), Bash(git branch:*), Bash(git add:*), Bash(git commit:*), Bash(gh issue view:*), Bash(dotnet format:*), Bash(dotnet build:*), Bash(dotnet test:*)
---

# Ship PR

Turn a reviewed, implemented issue into a pull request. Never push or open the PR without the user's go-ahead.

1. **Gates**: confirm with `gh-compact-view` that the issue has an `## Implementation Run Log` and a `## Code Review Report` with no unresolved 🔴 finding (skill `agent-handoffs`). If not, stop and name the missing step.
2. **Branch**: must not be `main`. Branch name should contain the issue number (`workitems/<n>`).
3. **Clean**: run `dotnet format ./TimeForCode.sln`, then a plain `dotnet build TimeForCode.sln` and `dotnet test TimeForCode.sln --no-build`. Stop on any failure. Markdown changed? Run the `markdown-linter` subagent first.
4. **Commit**: `git status` / `git diff --stat`, stage only files that belong to the issue (never `.agent-state/`, `.env*`, secrets). Conventional-commit subject (`feat:`/`fix:`/`docs:` + scope, `(#<n>)` suffix; release-please reads these). End the message with the attribution trailer from the session's system reminder.
5. **Confirm**: show the user the commit list and PR title/body draft; ask before pushing.
6. **PR**: `git push -u origin <branch>` then `gh pr create --title ... --body ...`. Body: summary (2-4 bullets), `Closes #<n>`, test evidence (TDD: tests added, seen red then green), docs touched, loose ends from the log. End with the PR attribution line. Report the PR URL.

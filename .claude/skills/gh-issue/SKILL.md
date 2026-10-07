---
name: gh-issue
description: "Create a GitHub issue, post a feature-file comment, or post an implementation run log via the GitHub CLI, after user approval. Use when the Plan phase has an approved issue draft, FeatureWriter has an approved Gherkin file, or Implementation has finished a session."
argument-hint: "create | comment-feature <issue> | impl-log <issue>"
allowed-tools: Bash(gh issue create:*), Bash(gh issue comment:*), Bash(gh issue view:*), Bash(gh label list:*), Bash(gh auth status), Bash(gh repo view:*)
---

# GitHub CLI issue operations

One skill, three modes. Always run the preflight first, then the section for your mode. Only post content the user has approved (issue draft, feature file); the implementation log needs no extra approval but must follow the structure below. Bodies go through `--body-file` in `$env:TEMP` (never the repository) to avoid here-string problems with backticks.

## Preflight (all modes)

```powershell
gh auth status
gh repo view --json nameWithOwner --jq '.nameWithOwner'
```

Expected repo: `wouterfennis/TimeForCode`. Not authenticated: stop and ask the user to run `gh auth login`. Wrong repo: ask the user to switch to the repository root.

## Mode `create` (Plan phase)

Input: approved title (`[Type]: description`) and body sections. Never run any other state-changing command.

1. `gh label list`; use only labels that exist (Feature/Improvement → `enhancement`, Bug Fix → `bug`, Technical Debt → `technical-debt`, Documentation → `documentation`). Missing label: closest existing one or omit.
1. Write `$env:TEMP/issue_body.md` with the Write tool, with sections: `## Motivation / Context`, `## Proposed Solution`, `## Affected Areas`, `## Acceptance Criteria` (keep checkboxes), `## Additional Context` (omit when empty). Templates for single, parent and child issues: skill `issue-draft-templates`.
1. Create, one `--label` flag per label (no commas):

```powershell
gh issue create --title "<exact approved title>" --body-file $env:TEMP/issue_body.md --label "<label1>" --label "<label2>"
Remove-Item $env:TEMP/issue_body.md
```

1. Report `ISSUE_CREATED` with number and URL. Errors: label not found → recheck labels; repository not found → rerun preflight.

## Mode `comment-feature` (FeatureWriter phase)

Input: approved Gherkin, issue number, lists of new and reused steps. Write `$env:TEMP/comment_body.md`:

````markdown
## Prepared Feature File

Intended path: `<tst/.../Features/X.feature>`

```gherkin
<full feature file content>
```

### New step definitions required

<bulleted steps without an existing implementation>

### Reused step definitions

<bulleted steps that already exist>
````

```powershell
gh issue comment <issue-number> --body-file $env:TEMP/comment_body.md
Remove-Item $env:TEMP/comment_body.md
```

Report `COMMENT_POSTED` with the URL. Remind the user to post `APPROVED: feature` after review.

## Mode `impl-log` (Implementation phase)

Post only after the final build/test attempt. Structure (the `review` agent and `agent-handoffs` depend on the exact heading):

```powershell
$log = @"
## Implementation Run Log

**Date:** <ISO date>
**Issue:** #<number>

### Completed

<bullets: finished items with relative file paths>

### TDD Evidence

<bullets: tests seen red first, then green; any test changed and why>

### Loose Ends

<bullets: every TODO(review) with file:line, red tests left, unreached plan items>

### Open Questions

<bullets: deferred decisions with context>
"@

gh issue comment <issue-number> --body $log
```

Report `LOG_POSTED` with the URL.

## Errors

| Symptom | Action |
| --- | --- |
| `gh: command not found` | Ask the user to install and authenticate the GitHub CLI |
| `Could not resolve to an issue` | Confirm the number with the user, retry |
| Authentication error | `gh auth login` |

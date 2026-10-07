---
name: gh-compact-view
description: "Fetch GitHub issue data with small jq projections instead of raw JSON with all comments. Use when: reading an issue, its acceptance criteria, a feature-file comment, or checking phase gates."
allowed-tools: Bash(gh issue view:*)
---

# gh-compact-view

`gh issue view <n> --json title,body,labels,comments` returns every comment in full (often 10k+ tokens). Fetch only what the task needs. Commands work in PowerShell and bash.

## 1. Issue overview (title, labels, body, comment index)

```powershell
gh issue view <n> --json number,title,labels,body,comments --jq '{number, title, labels: [.labels[].name], body, comments: [.comments | to_entries[] | {i: .key, a: .value.author.login, h: (.value.body | split("\n")[0] | rtrimstr("\r"))}]}'
```

## 2. One comment in full (by index from step 1)

```powershell
gh issue view <n> --json comments --jq '.comments[<i>].body'
```

## 3. The feature file (latest gherkin comment)

```powershell
gh issue view <n> --json comments --jq '[.comments[] | select(.body | contains("```gherkin"))] | last | .body'
```

## 4. Gate check (flags only, no bodies)

```powershell
gh issue view <n> --json comments --jq '[.comments[] | {a: .author.login, h: (.body | split("\n")[0] | rtrimstr("\r")), gherkin: (.body | contains("```gherkin")), ok: (.body | test("approved|lgtm|looks good|✅|:white_check_mark:"; "i"))}]'
```

## Rules

- Start with 1; fetch full comments only when needed. Never re-fetch an issue already in context.
- Add `--jq` to every `gh issue list` / `gh pr view` too (e.g. `--jq '.[] | "\(.number) \(.title)"'`).
- Posting comments is unchanged: use skill `gh-issue`.

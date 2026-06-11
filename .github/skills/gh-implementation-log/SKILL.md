---
name: gh-implementation-log
description: Posts an implementation run log as a comment on an existing GitHub issue using the GitHub CLI. Use this skill when the Implementation agent has finished a work session and needs to record what was done and what loose ends remain.
---

# GitHub CLI Implementation Log

This skill posts the end-of-session implementation log as a comment on a GitHub issue via the `gh` CLI. Follow every step in order.

---

## Step 1 — Verify Authentication

```powershell
gh auth status
```

**Expected output:** A message indicating you are logged in to `github.com` as a valid user.

**If not authenticated:** Stop and instruct the user to run `gh auth login`, then wait for confirmation before continuing.

---

## Step 2 — Verify Repository Context

```powershell
gh repo view --json nameWithOwner --jq '.nameWithOwner'
```

**Expected output:** `wouterfennis/TimeForCode`

If the output is different, instruct the user to navigate to the repository root before continuing.

---

## Step 3 — Post the Log Comment

Use a here-string to preserve formatting. The comment must use the exact structure below so the log is easy to scan.

```powershell
$log = @"
## Implementation Run Log

**Date:** <ISO date>
**Issue:** #<number>

---

### Completed

<bulleted list of work items that were fully implemented and verified>

---

### Loose Ends

<bulleted list of items that are incomplete, need review, or were marked with TODO/REVIEW comments — include file path and line reference for each>

---

### Open Questions

<bulleted list of decisions deferred pending user or domain input — include context for each>
"@

gh issue comment <issue-number> --body $log
```

Replace `<issue-number>` with the actual issue number and fill in all three sections from the agent's summary.

**Confirm success:** The command outputs the URL of the new comment. Report this URL to the user.

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `gh: command not found` | Instruct user to install the GitHub CLI and authenticate |
| `Could not resolve to an issue` | Confirm the issue number with the user and retry |
| Authentication error | Instruct user to run `gh auth login` |

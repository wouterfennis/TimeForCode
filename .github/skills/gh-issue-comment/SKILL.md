---
name: gh-issue-comment
description: Posts a prepared feature file as a comment on an existing GitHub issue using the GitHub CLI. Use this skill when the FeatureWriter agent has a fully approved Gherkin feature file ready to attach to an issue.
---

# GitHub CLI Issue Comment

This skill posts an approved Gherkin feature file as a comment on a GitHub issue via the `gh` CLI. Follow every step in order.

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

If the output is different or an error occurs, instruct the user to navigate to the repository root before continuing.

---

## Step 3 — Post the Comment

Use a here-string to preserve the Gherkin formatting exactly. Wrap the feature file content in a fenced code block tagged `gherkin` so GitHub renders it with syntax highlighting.

```powershell
$comment = @"
## Prepared Feature File

``````gherkin
<full feature file content here>
``````

### New step definitions required

<bulleted list of steps that do not have an existing implementation>

### Reused step definitions

<bulleted list of steps that already have implementations>
"@

gh issue comment <issue-number> --body $comment
```

Replace `<issue-number>` with the actual issue number, and fill in the three sections from the approved draft.

**Confirm success:** The command outputs the URL of the new comment. Report this URL to the user.

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `gh: command not found` | Instruct user to install the GitHub CLI and authenticate |
| `Could not resolve to an issue` | Confirm the issue number with the user and retry |
| Authentication error | Instruct user to run `gh auth login` |

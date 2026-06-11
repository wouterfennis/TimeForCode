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

Write the comment body to a temporary file, then pass it to `gh` with `--body-file`. This avoids PowerShell here-string issues with multiline content containing backticks or special characters.

1. Use `create_file` to write the comment body to `comment_body.md` at the repository root. The file must follow this structure:

```markdown
## Prepared Feature File

```gherkin
<full feature file content here>
```

### New step definitions required

<bulleted list of steps that do not have an existing implementation>

### Reused step definitions

<bulleted list of steps that already have implementations>
```

2. Post the comment:

```powershell
gh issue comment <issue-number> --body-file comment_body.md
```

3. Delete the temporary file immediately after:

```powershell
Remove-Item comment_body.md
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

---
name: gh-issue-create
description: Creates a GitHub issue using the GitHub CLI (gh) after user approval. Use this skill when the plan agent has a fully approved issue draft ready to submit to GitHub. Handles authentication verification, label validation, and issue submission.
---

# GitHub CLI Issue Creation

This skill guides you through submitting an approved issue draft to GitHub via the `gh` CLI. Follow every step in order. Do not skip steps.

---

## Step 1 — Verify Authentication

Run the following command to confirm the GitHub CLI is authenticated and targeting the correct repository:

```powershell
gh auth status
```

**Expected output:** A message indicating you are logged in to `github.com` as a valid user.

**If not authenticated:** Stop and instruct the user to run `gh auth login`, then wait for them to confirm authentication before continuing.

---

## Step 2 — Verify Repository Context

Confirm the CLI is operating in the correct repository:

```powershell
gh repo view --json nameWithOwner --jq '.nameWithOwner'
```

**Expected output:** `wouterfennis/TimeForCode`

If the output is different or an error occurs, instruct the user to navigate to the repository root before proceeding.

---

## Step 3 — Check Available Labels

Fetch the list of labels that exist in the repository so you can assign only valid ones:

```powershell
gh label list
```

Use the output to select the most appropriate labels for the issue. Do not invent label names. Common mappings:

| Issue Type | Suggested Label |
|-----------|----------------|
| Feature | `enhancement` |
| Bug Fix | `bug` |
| Improvement | `enhancement` |
| Technical Debt | `technical-debt` |
| Documentation | `documentation` |

If a suggested label does not exist, use the closest available label or omit it.

---

## Step 4 — Build and Submit the Issue

Write the issue body to a temporary file, then pass it to `gh issue create` with `--body-file`. This avoids PowerShell here-string issues with multiline content containing backticks or special characters.

1. Use `create_file` to write the issue body to `issue_body.md` at the repository root. The file must follow this structure:

```markdown
## Motivation / Context

[Replace with the full Motivation / Context section from the approved draft]

## Proposed Solution

[Replace with the full Proposed Solution section from the approved draft]

## Affected Areas

[Replace with the full Affected Areas section from the approved draft]

## Acceptance Criteria

[Replace with the full Acceptance Criteria section from the approved draft, preserving checkboxes]

## Additional Context

[Replace with the Additional Context section, or omit the section header if empty]
```

2. Create the issue, referencing the file:

```powershell
gh issue create `
  --title "[Replace with the exact approved title, including the [Type]: prefix]" `
  --body-file issue_body.md `
  --label "[label1]" `
  --label "[label2]"
```

> **Note:** Add one `--label` flag per label. Do not combine multiple labels in a single `--label` argument using commas.

3. Delete the temporary file immediately after:

```powershell
Remove-Item issue_body.md
```

---

## Step 5 — Confirm and Report

The `gh issue create` command outputs the URL of the newly created issue upon success. Report this URL to the user in the format:

```
Issue created successfully: https://github.com/wouterfennis/TimeForCode/issues/[number]
```

If the command fails, read the error message carefully:
- **Authentication error** → Repeat Step 1
- **Label not found** → Repeat Step 3 and correct the label names
- **Repository not found** → Repeat Step 2
- **Network error** → Advise the user to check their internet connection and retry

---

## Safety Reminders

- This skill is the **only** permitted use of `run_in_terminal` in the Plan Agent.
- Never run any command that modifies files, stages changes, creates branches, or alters repository state.
- If in doubt about a command's effect, do not run it and ask the user instead.

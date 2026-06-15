---
name: markdown-lint
description: Runs markdownlint-cli against all Markdown files in the repository, analyses violations, and applies safe automatic and manual fixes. Use this skill when the MarkdownLinter agent needs to perform a full lint-fix-report cycle.
---

# Markdown Lint Skill

This skill runs a full lint-fix-scan cycle for all Markdown files in the **TimeForCode** repository. Follow every step in order.

---

## Step 1 — Verify markdownlint-cli Is Available

```powershell
markdownlint --version
```

**Expected output:** Version `0.48.0` (to match CI).

**If not found:** Stop and report:
> "`markdownlint-cli@0.48.0` is not installed. Install it with `npm install -g markdownlint-cli@0.48.0` and re-run this agent."

Do not attempt to install it yourself.

---

## Step 2 — Run the Full Lint Scan

Run markdownlint against all Markdown files, respecting the project config at `.markdownlint.json`:

```powershell
markdownlint "**/*.md" --ignore node_modules --ignore .git 2>&1
```

Capture the full output and the exit code. If the exit code is `0`, record that no violations were found and jump to Step 6.

---

## Step 3 — Analyse the Output

Each output line follows this format:

```
<file>:<line> <rule-id>/<rule-alias> <description>
```

Group violations by file. For each file compute:

- Total violation count
- List of unique rule IDs triggered

Produce a summary table:

| File | Violations | Rules triggered |
|------|-----------|-----------------|
| `docs/architecture/arc42/01-introduction-and-goals.md` | 3 | MD013, MD041 |
| … | … | … |

Then list the full per-file detail beneath the table.

---

## Step 4 — Apply Automatic Fixes

Run markdownlint with `--fix` to apply formatting corrections that do not alter document meaning:

```powershell
markdownlint "**/*.md" --ignore node_modules --ignore .git --fix 2>&1
```

Re-run the scan to capture any remaining violations:

```powershell
markdownlint "**/*.md" --ignore node_modules --ignore .git 2>&1
```

---

## Step 5 — Apply Manual Fixes for Remaining Violations

For each violation that `--fix` could not resolve, open the offending `.md` file and correct it directly. Only fix what `markdownlint` flagged — do not rewrite surrounding prose, restructure headings, or alter document meaning.

**Safe manual corrections include:**

- Adding or removing blank lines around headings, lists, or code fences
- Correcting heading levels to be consistent
- Replacing bare URLs with `[text](url)` links when the rule requires it
- Adding a language identifier to fenced code blocks
- Fixing list marker consistency (all `-` or all `*`)

If a remaining violation would require changing the intended meaning of the text, do not fix it — mark it as requiring human decision.

After all manual fixes, run the final scan:

```powershell
markdownlint "**/*.md" --ignore node_modules --ignore .git 2>&1
```

Record:

- Violations resolved by auto-fix
- Violations resolved by manual edit
- Violations remaining for human decision

---

## Step 6 — Report Results

Present a summary to the user:

| Metric | Value |
|--------|-------|
| Files scanned | N |
| Files with violations | N |
| Total violations (before fix) | N |
| Auto-fixed | N |
| Manually fixed | N |
| Remaining (requires human decision) | N |

If violations remain that require human decision, list them grouped by file with their rule IDs and descriptions.

If the scan was completely clean from the start, report:
> ✅ No Markdown violations found. All files comply with `.markdownlint.json`.

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `markdownlint: command not found` | Instruct user to install with `npm install -g markdownlint-cli` |

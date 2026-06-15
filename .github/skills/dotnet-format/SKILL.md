---
name: dotnet-format
description: 'Runs dotnet format on the TimeForCode solution to fix whitespace, import ordering, and final newline issues. Use when: fixing code style errors, fixing WHITESPACE errors, fixing IMPORTS ordering, fixing FINALNEWLINE errors, running dotnet format, formatting the solution, fixing formatter CI failures.'
argument-hint: 'Optionally specify a severity level (info, warn, error) or a specific project path'
---

# dotnet-format Skill

This skill runs `dotnet format` against the **TimeForCode** solution to fix code style violations. Follow every step in order.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `team` |
| Status | `active` |
| Overlap risk | `none` |
| Review cadence | `on-change` |

---

## Trigger Conditions

Invoke this skill when:

- CI reports `WHITESPACE`, `IMPORTS`, or `FINALNEWLINE` formatting errors
- Before committing to ensure code style compliance
- After resolving merge conflicts that may have introduced formatting issues

---

## Required Inputs

| Input | Source |
|-------|--------|
| Authenticated `dotnet` SDK | Verified before running any command |
| `TimeForCode.sln` at repository root | Must exist |

---

## Expected Outputs

- All auto-fixable style violations resolved
- Build verified clean after formatting
- List of changed files provided via `git diff --name-only`

---

## Step 1 — Run the Formatter

Run the formatter against the whole solution:

```powershell
dotnet format ./TimeForCode.sln
```

If only specific diagnostics need fixing, target them explicitly:

```powershell
dotnet format ./TimeForCode.sln --diagnostics IDE0005   # unused imports
dotnet format ./TimeForCode.sln --severity warn
```

---

## Step 2 — Verify the Build

After formatting, verify no build errors were introduced:

```powershell
dotnet build ./TimeForCode.sln
```

If the build fails, investigate the formatter changes and revert any that caused a regression.

---

## Step 3 — Review Changed Files

Review and report the changed files:

```powershell
git diff --name-only
```

Present the list of changed files to the calling agent or user.

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `dotnet` not found | Stop and report .NET SDK is not installed |
| Build fails after formatting | Revert formatting changes and report which file caused the regression |
| No files changed | Report that all files already comply with the style rules |

---

## Notes

- `dotnet format` modifies files in-place — commit or stash existing changes first if needed.
- The solution file is at the repository root: `TimeForCode.sln`.
- StyleCop / EditorConfig rules drive the exact fixes applied.


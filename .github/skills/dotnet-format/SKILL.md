---
name: dotnet-format
description: 'Runs dotnet format on the TimeForCode solution to fix whitespace, import ordering, and final newline issues. Use when: fixing code style errors, fixing WHITESPACE errors, fixing IMPORTS ordering, fixing FINALNEWLINE errors, running dotnet format, formatting the solution, fixing formatter CI failures.'
argument-hint: 'Optionally specify a severity level (info, warn, error) or a specific project path'
---

# dotnet-format

## When to Use

- CI reports `WHITESPACE`, `IMPORTS`, or `FINALNEWLINE` formatting errors
- Before committing to ensure code style compliance
- After resolving merge conflicts that may have introduced formatting issues

## Procedure

1. Run the formatter against the whole solution:

```powershell
dotnet format ./TimeForCode.sln
```

2. If only specific diagnostics need fixing, target them explicitly:

```powershell
dotnet format ./TimeForCode.sln --diagnostics IDE0005   # unused imports
dotnet format ./TimeForCode.sln --severity warn
```

3. After formatting, verify no build errors were introduced:

```powershell
dotnet build ./TimeForCode.sln
```

4. Review and stage the changed files:

```powershell
git diff --name-only
```

## Notes

- `dotnet format` modifies files in-place — commit or stash existing changes first if needed.
- The solution file is at the repository root: `TimeForCode.sln`.
- StyleCop / EditorConfig rules drive the exact fixes applied.

---
paths:
  - "**/*.cs"
---

# Code Style Instructions

This file defines the mandatory code-style gate for every agent or contributor that writes or modifies C# files in the **TimeForCode** repository.

---

## Mandatory: Run `dotnet format` Before Every Commit

**Every agent must run `dotnet format` on the solution before committing any C# changes.**

```bash
dotnet format ./TimeForCode.sln
```

A `PostToolUse` hook already formats every `.cs` file you edit or write (`dotnet format whitespace --folder --include <file>`, whitespace and editorconfig only). The solution-wide run above (skill `dotnet-format`) is still required before `git commit`.

### When to run

| Situation | Action |
| --- | --- |
| After writing new C# files | Run `dotnet format` |
| After editing existing C# files | Run `dotnet format` |
| After resolving merge conflicts in C# files | Run `dotnet format` |
| Before calling `git commit` / committing | Run `dotnet format` |

### Verify the build after formatting

After a solution-wide `dotnet format`, run one plain `dotnet build TimeForCode.sln` (skill `quiet-dotnet`; no `--no-incremental` unless a stale build is suspected). If the build breaks, fix the regression before committing.

---

## Project Style Conventions (driven by `.editorconfig`)

| Rule | Value |
| --- | --- |
| Indent style | spaces, size 4 |
| End of line | `lf` |
| Final newline | `false` (no trailing newline at end of file) |
| Namespace style | `block_scoped` |
| Using directive placement | `outside_namespace` |
| `var` usage | explicit types preferred (`false` for all `var` preferences) |

These rules are enforced automatically by `dotnet format` — **do not override them manually**.

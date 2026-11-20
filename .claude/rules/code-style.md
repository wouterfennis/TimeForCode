---
paths:
  - "**/*.cs"
---

# Code Style Instructions

This file defines the mandatory code-style gate for every agent or contributor that writes or modifies C# files in the **TimeForCode** repository.

---

## Mandatory: Run `dotnet format` as the last step

**Every agent must run `dotnet format` on the solution once, as the last step before the final commit/PR, not before each intermediate step commit.**

```bash
dotnet format ./TimeForCode.sln
```

A `PostToolUse` hook already formats every `.cs` file you edit or write (`dotnet format whitespace --folder --include <file>`, whitespace and editorconfig only). The solution-wide run above (skill `dotnet-format`) is still required once at the end of the work.

### When to run

| Situation | Action |
| --- | --- |
| Last step of the work, before shipping | Run `dotnet format` |
| After resolving merge conflicts in C# files | Run `dotnet format` |
| Intermediate step commits | Skip (the hook covers edited files) |

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

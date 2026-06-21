---
applyTo: "**"
---

# Code Style Instructions

This file defines the mandatory code-style gate for every agent or contributor that writes or modifies C# files in the **TimeForCode** repository.

---

## Mandatory: Run `dotnet format` Before Every Commit

**Every agent must run `dotnet format` on the solution before committing any C# changes.**

```bash
dotnet format ./TimeForCode.sln
```

This is enforced by the `dotnet-format` skill. Invoke it after writing or editing any `.cs` file and before calling `report_progress`.

### When to run

| Situation | Action |
|-----------|--------|
| After writing new C# files | Run `dotnet format` |
| After editing existing C# files | Run `dotnet format` |
| After resolving merge conflicts in C# files | Run `dotnet format` |
| Before calling `report_progress` / committing | Run `dotnet format` |

### Verify the build after formatting

After running `dotnet format`, always confirm the build is still clean:

```bash
dotnet build ./TimeForCode.sln --no-incremental
```

If the build breaks after formatting, investigate the changed files and fix the regression before committing.

---

## Project Style Conventions (driven by `.editorconfig`)

| Rule | Value |
|------|-------|
| Indent style | spaces, size 4 |
| End of line | `lf` |
| Final newline | `false` (no trailing newline at end of file) |
| Namespace style | `block_scoped` |
| Using directive placement | `outside_namespace` |
| `var` usage | explicit types preferred (`false` for all `var` preferences) |

These rules are enforced automatically by `dotnet format` — **do not override them manually**.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `team` |
| Status | `active` |
| Overlap risk | `none` |
| Review cadence | `on-change` |

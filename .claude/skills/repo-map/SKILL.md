---
name: repo-map
description: "Compact project/layer listing of the TimeForCode solution so agents skip directory exploration. Use when: locating a project, finding where a layer or test project lives, or when the map in CLAUDE.md looks stale."
---

# repo-map

The static map is in `CLAUDE.md`. For the live list of projects, run (about 40 lines instead of exploring folders):

```powershell
pwsh -NoProfile -File .claude/skills/repo-map/list-projects.ps1            # all modules
pwsh -NoProfile -File .claude/skills/repo-map/list-projects.ps1 -Module Donation
```

Output: one line per project, `module | layer/kind | path`.

## Workflow

1. Run the script (optionally with `-Module`).
2. Use grep on the target project folder; read only matching line ranges.
3. If a project or layer is missing from `CLAUDE.md`, tell the user instead of editing it silently.

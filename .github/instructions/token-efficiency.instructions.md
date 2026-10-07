---
applyTo: "**"
description: Rules that keep agent context small. Always on.
---

# Token efficiency

- Search first (`grep`/file search), then read only the needed line range. Never read whole large files "to look around".
- Never read or list `node_modules`, `obj`, `.git`, lockfiles, binaries or `docs/images` (a hook blocks it).
- Do not re-read a file you just edited, and do not re-fetch an issue already in context.
- Build/test one project at a time; use `--no-restore`/`--no-build` when nothing changed. Output is trimmed by a hook; the full log path is printed on failure.
- Fetch GitHub issues with a `--jq` projection, not raw `--json ...comments` (skill `gh-compact-view`).
- Reply tersely: no restating the plan, no summaries of diffs the user can see.
- Long task or context getting full: write progress to `.agent-state/handoff.md` (skill `context-handoff`).

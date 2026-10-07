---
name: context-handoff
description: "Write and resume a short work-state note (.agent-state/handoff.md) so long tasks survive context compaction or a new session. Use when: a task spans many steps, context is getting large, or resuming after a break."
---

# context-handoff

A `PreCompact` hook snapshots branch and changed files into `.agent-state/handoff.md` (git-ignored); a `SessionStart` hook re-injects its first 40 lines. The agent owns the `## Notes` section.

## Writing notes (do this at each phase boundary, and before a long tool run)

Replace the `## Notes` section of `.agent-state/handoff.md` with at most 15 lines:

```markdown
## Notes
Issue: #<n> — <title>
Phase: Implementation (step 5c of 8)
Done: <files/steps finished, one line each>
Next: <the single next action>
Open questions: <blocking decisions, or none>
Commands: <exact test command that is currently failing, if any>
```

If the file does not exist yet, create it with only the `## Notes` section; the hook adds the snapshot above it later.

## Resuming

1. Read `.agent-state/handoff.md` (already injected by the SessionStart hook in most tools).
2. Trust the note over re-exploring; verify only the "Next" item.
3. When the issue is finished, delete the file.

## Rules

- Never put secrets or full code in the note — paths and one-line statements only.
- Keep it under ~300 tokens; overwrite, do not append.

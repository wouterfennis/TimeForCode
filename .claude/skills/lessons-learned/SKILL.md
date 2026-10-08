---
name: lessons-learned
description: "Read and update the persisted lessons file (.claude/lessons-learned.md) so mistakes and discoveries survive between sessions. Use when: starting an implementation or fix task (read), finishing a task or after a correction or surprise (write)."
---

# lessons-learned

Persistent, committed store of non-obvious repo knowledge: [.claude/lessons-learned.md](../../lessons-learned.md). Markdown on purpose: greppable, reviewable in PRs, shared with the team. Do not use `.agent-state/` for this (git-ignored, per-session).

## Read (start of implement, fix or review work)

Grep the file for the area you will touch (`Grep` on the module, tool or keyword) and read only the matching entries. Do not read the whole file unless it is short.

## Write (end of a task, or right after a user correction, failed approach or surprise)

Add an entry only when it is non-obvious and not already derivable from code, git history, CLAUDE.md or `.claude/rules/`. Skip task progress and one-off facts.

1. Grep for an existing entry on the same topic; update it instead of adding a duplicate. Delete entries that turned out wrong.
2. Append under the matching `##` category, newest last, using this exact shape:

```markdown
### <Short imperative title>

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** <the fact or rule, one or two sentences>
- **Why:** <what went wrong or was learned; the cost of ignoring it>
- **How to apply:** <when it triggers and what to do>
```

- Keep entries under 8 lines. Never record secrets, tokens or personal data.
- Mention new entries in the final reply in one line.

## Housekeeping

When the file passes about 40 entries, merge duplicates and drop stale ones before adding more.

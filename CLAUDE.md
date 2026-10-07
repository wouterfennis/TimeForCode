@.github/copilot-instructions.md
@.github/instructions/token-efficiency.instructions.md

# Claude Code notes

This repo's agents, skills and instructions live in `.github/` (shared with GitHub Copilot). Claude Code does not auto-load them, so:

- Skills: `.github/skills/<name>/SKILL.md` — read the one you need when its description matches the task; do not load them all.
- Agent role definitions: `.github/agents/*.agent.md` — read only when asked to act as that agent.
- Other instructions (`code-style`, `testing-strategy`, `agent-handoffs`): `.github/instructions/` — read when writing C#, writing tests, or crossing a workflow phase boundary.
- Hooks and permissions for this repo are in `.claude/settings.json` (scripts in `.github/hooks/scripts/`). Do not also enable `chat.useClaudeHooks` in VS Code; `.github/hooks/token-saving.json` already covers it.

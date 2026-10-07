---
paths:
  - "docs/**/*.md"
  - "*.md"
---

# Documentation rules

- Markdown must pass `markdownlint` with the repo's `.markdownlint.json` (use the `markdown-linter` subagent; do not pass conflicting rule flags).
- `docs/current/` describes **implemented** behaviour only; plans and ideas do not belong there.
- arc42 chapters live in `docs/architecture/arc42/`. Update the chapter when you add a component (05), runtime flow (06), infra (07), cross-cutting pattern (08), decision (09) or known shortcut / `TODO(review)` (11). Run skill `doc-align` after large changes.
- Never read `docs/images/`; reference images by link only.
- Keep headings sentence-case, one H1 per file, fenced code blocks with a language.

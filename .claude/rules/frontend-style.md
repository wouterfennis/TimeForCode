---
paths:
  - "src/Frontend/TimeForCode.Frontend/**/*.ts"
  - "src/Frontend/TimeForCode.Frontend/**/*.tsx"
---

# Front-end code style

## Comments above declarations

Put a `/** ... */` doc comment above every hand-written declaration (function, class, type, interface, exported or module-level const, class and interface members). A plain `//` comment directly above a declaration does not show in editor tooltips and **fails `npm run lint`** (rule `timeforcode/doc-comment-above-declaration`, `lint/declaration-comments-plugin.js`).

`//` stays allowed inside function bodies, above object properties, as `// TODO...` markers (`// TODO(review): ...`), as tool directives (`@ts-expect-error`, `oxlint-disable-next-line`, `prettier-ignore`, `#region`), and above a declaration when a blank line separates them. Generated `src/api/*.d.ts` files are excluded.

Details: `docs/frontend/architecture.md` ("Comments above declarations"). The C# rule is in `.claude/rules/code-style.md`.

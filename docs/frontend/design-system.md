# Design System

Status: Draft

A small, token-driven design system. The values below are a **starting proposal** (primary blue `#1b6ec2`). A designer may change any value; keep the token names stable, because components refer to names, never raw values.

---

## Principles

The visual direction is a toned-down game store: inspired by the browsing experience of stores such as Steam and the responsive feel of console interfaces, without copying their look and without their flashiness.

- **A store you want to browse.** Projects are presented like titles in a game store: imagery first, shelves by theme, and clear badges for what matters (help needed, hours pledged).
- **Motion that responds, not motion that performs.** Tiles react to hover and keyboard focus alike, and transitions show where content comes from and goes. Motion comes in two layers. *Primary* motion answers input directly (a tile lifting, a page changing) and lasts 300 ms or less. *Secondary* motion follows it and may take its time with a long, slowing ease-out (a light sweep over tile art, the spotlight background settling), because it never blocks input. Motion is small (a tile grows by at most about 5%), never loops or plays on its own, and is turned off when the user prefers reduced motion.
- **Trustworthy where it counts.** Hours and progress are accurate and easy to read. No store tricks: no countdowns, fake urgency or scarcity messages.
- **Content first.** The project, its image and its need are the hero; decoration only supports them.
- **Dark first, driven by tokens.** The dark theme is the default; a light theme can follow from the same token names. The token values below are the original light proposal and are replaced by the dark set in [#211](https://github.com/wouterfennis/TimeForCode/issues/211).

## Design tokens

Tokens are CSS custom properties defined once on `:root`. Components must never use raw colour values.

| Token | Value | Use |
| --- | --- | --- |
| `--color-bg` | `#ffffff` | Page background |
| `--color-surface` | `#f6f8fa` | Cards, panels |
| `--color-border` | `#d0d7de` | Dividers, outlines |
| `--color-text` | `#1f2328` | Body text |
| `--color-text-muted` | `#59636e` | Secondary text |
| `--color-primary` | `#1b6ec2` | Buttons, links, focus |
| `--color-primary-contrast` | `#ffffff` | Text on primary |
| `--color-success` | `#1a7f37` | Positive status |
| `--color-warning` | `#9a6700` | Caution |
| `--color-danger` | `#cf222e` | Errors, destructive actions |

Check every text/background pair for at least 4.5:1 contrast (3:1 for large text and UI boundaries). Adjust values rather than skipping the check.

| Scale | Values |
| --- | --- |
| Spacing | 4, 8, 12, 16, 24, 32, 48, 64 px (`--space-1` to `--space-8`) |
| Radius | 4 px (controls), 8 px (cards), 999 px (pills) |
| Type | System font stack; sizes 14, 16, 18, 20, 24, 32 px; line height 1.5 body, 1.25 headings |
| Breakpoints | 640, 960, 1280 px; design mobile first |
| Elevation | None by default; one subtle shadow for dialogs and menus |

## Component inventory

Build these once, in a shared folder, and compose pages from them.

| Component | Notes |
| --- | --- |
| Button | Primary, secondary, danger, link; loading and disabled states |
| Link | Distinguish external links (opens GitHub) with an icon and `rel="noopener noreferrer"` |
| TextField, TextArea, Select, Checkbox | Label always visible; error text tied to the field with `aria-describedby` |
| Form | Inline validation, summary of errors at the top on submit |
| Card | Used by project tiles |
| Table | Responsive; becomes stacked rows on small screens |
| Pagination | Reflects page in the URL |
| Dialog | Focus trap, Escape to close, returns focus; used for confirmations |
| Toast / Alert | Success and error feedback with `role="status"` / `role="alert"` |
| Skeleton | Loading placeholders with the same layout as the loaded content, so nothing jumps when it arrives (layout shift close to 0; in the app, measure Cumulative Layout Shift in the E2E tests) |
| EmptyState, ErrorState | Icon, message, optional action |
| Avatar | With text fallback |
| Badge | Language, status |
| AppShell | Header, navigation, footer, skip link |

## Accessibility rules (WCAG 2.2 AA)

1. Every interactive element is reachable and operable by keyboard, with a visible focus ring using `--color-primary`.
2. A "Skip to content" link is the first focusable element.
3. One `h1` per page; headings do not skip levels.
4. Use native elements (`button`, `a`, `table`, `form`) before ARIA.
5. Never rely on colour alone to convey state.
6. Respect `prefers-reduced-motion`.
7. Touch targets are at least 24x24 px (prefer 44x44).
8. Run automated axe checks in component and E2E tests, and do a manual keyboard pass on every new page.

## Iconography and imagery

One icon set (an open-source set such as Lucide), SVG only, decorative icons hidden from assistive technology. The logo, lockup, colours and usage rules are in the [brand kit](../branding/README.md); the front-end does not use the logo yet. **Open question for the designer**: final accent colour for the interface.

## Content style

Plain English, short sentences, active voice (see [docs conventions](../README.md#language-and-style)). Buttons describe the action ("Publish project", not "Submit"). Error messages say what happened and what to do next.

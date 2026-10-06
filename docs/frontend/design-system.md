# Design System

Status: Draft

A small, token-driven design system. The values below are a **starting proposal** derived from the colours already used in the Blazor site (`#1b6ec2` primary blue). A designer may change any value; keep the token names stable, because components refer to names, never raw values.

---

## Principles

- Calm and trustworthy. Plenty of whitespace, one accent colour, no decoration for its own sake.
- Content first. Project names and descriptions are the hero.
- Light and dark themes from day one, driven by tokens.

## Design tokens

Tokens are CSS custom properties defined once on `:root`, with dark values under `prefers-color-scheme: dark` and a manual `data-theme` override. Components must never use raw colour values.

| Token | Light | Dark | Use |
| --- | --- | --- | --- |
| `--color-bg` | `#ffffff` | `#0f1419` | Page background |
| `--color-surface` | `#f6f8fa` | `#171d24` | Cards, panels |
| `--color-border` | `#d0d7de` | `#2d363f` | Dividers, outlines |
| `--color-text` | `#1f2328` | `#e6edf3` | Body text |
| `--color-text-muted` | `#59636e` | `#9198a1` | Secondary text |
| `--color-primary` | `#1b6ec2` | `#4493f8` | Buttons, links, focus |
| `--color-primary-contrast` | `#ffffff` | `#0f1419` | Text on primary |
| `--color-success` | `#1a7f37` | `#3fb950` | Positive status |
| `--color-warning` | `#9a6700` | `#d29922` | Caution |
| `--color-danger` | `#cf222e` | `#f85149` | Errors, destructive actions |

Check every text/background pair for at least 4.5:1 contrast (3:1 for large text and UI boundaries) in both themes. Adjust values rather than skipping the check.

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
| Skeleton | Loading placeholders |
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

One icon set (an open-source set such as Lucide), SVG only, decorative icons hidden from assistive technology. The project banner at `docs/images/banner.png` can inform the brand, but no logo or brand mark is final. **Open question for the designer**: logo, wordmark, and final accent colour.

## Content style

Plain English, short sentences, active voice (see [docs conventions](../README.md#language-and-style)). Buttons describe the action ("Publish project", not "Submit"). Error messages say what happened and what to do next.

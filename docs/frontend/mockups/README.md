# Design Mockups

Status: Draft

Code-based mockups of the new front-end, kept in the repository so they are reviewed, versioned and diffed like any other change. They are plain HTML and CSS with no framework, so they stay independent of the [React stack decision](../../reference/adr/0012-react-spa-frontend.md).

The mockups implement the proposal in the [design system](../design-system.md) and the wireframes in the [page specifications](../page-specs.md). Colours, spacing and radii live in one place, [tokens.css](tokens.css), using the same token names as the design system. Change a token there and every page follows.

## Pages

| Mockup | Page spec | Screenshots |
| --- | --- | --- |
| [index.html](index.html) | Home | `screenshots/index.*.png` |
| [projects.html](projects.html) | Project list | `screenshots/projects.*.png` |
| [project-detail.html](project-detail.html) | Project detail, with the unpublish dialog | `screenshots/project-detail.*.png` |
| [profile.html](profile.html) | Profile | `screenshots/profile.*.png` |
| [admin-donor-organizations.html](admin-donor-organizations.html) | Donor organizations list and form errors | `screenshots/admin-donor-organizations.*.png` |
| [states.html](states.html) | Loading, empty, error and unauthorized states | `screenshots/states.*.png` |

Screenshot names follow `<page>.<desktop|mobile>.png`.

### Version 2 (game-store direction)

The [v2](v2/) folder holds the mockups for the toned-down game-store direction (#218). They share [v2/tokens.css](v2/tokens.css), [v2/components.css](v2/components.css) and the sample data in [v2/mockup.js](v2/mockup.js); the cards show only fields the API returns. A dashed **Mockup state** panel switches between states such as loading, empty and error; it is not part of the design.

| Mockup | Page spec | Issue |
| --- | --- | --- |
| [v2/moodboard.html](v2/moodboard.html) | Visual direction: references, palette, components, motion | #218 |
| [v2/home.html](v2/home.html) | Home | #216 |
| [v2/projects.html](v2/projects.html) | Project list with search, filter and paging | #213, #214 |
| [v2/project-detail.html](v2/project-detail.html) | Project detail | #215 |

## Tooling

Requires Node.js 20 or later. Run these from this folder.

```powershell
npm install
npx playwright install chromium   # first time only

npm start                         # browse at http://localhost:4173
npm run screenshots               # regenerate screenshots/
npm run check                     # axe accessibility check (WCAG 2.2 AA, includes contrast) and skeleton alignment
```

If Playwright's own browser cannot be installed or started, run the scripts with the installed Chrome: `$env:PW_CHANNEL = "chrome"` (PowerShell) or `PW_CHANNEL=chrome` (Bash).

Run `npm run check` before committing a change. It fails when a token change breaks contrast, and when a loading skeleton does not line up with the content it stands in for (see Conventions). Run `npm run screenshots` and commit the updated images with the change, so reviewers see the visual diff in the pull request.

## Conventions

- Use tokens only. Never put a raw colour in a page.
- Keep the `data-testid` values from [page-specs.md](../page-specs.md) on the matching elements.
- `shell.js` renders the shared header and footer. Set `data-user` to `visitor`, `user` or `admin` on `<body>` to preview each variant.
- Content marked *Planned* or *Open question* in the page specs is labelled in the mockup too. Do not treat it as a promise.
- A loading skeleton has the same layout as the loaded page. Give each skeleton block and the real element it stands in for the same `data-frame` name; `npm run check` then compares their left edges (within 8 px) and heights (within 32 px, because real text varies in length) at desktop and mobile width.
- Mockups show the target design. Once a page is built, the real page is the source of truth and the mockup should be updated to match, or removed.

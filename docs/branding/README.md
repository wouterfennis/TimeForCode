# TimeForCode brand kit

The logo is a progress ring around a code prompt. The ring is the time: the amber arc is the share of donated hours that has been logged, the same measure as the progress badge. The prompt `>_` is the code. Because the ring can show a real percentage, the same mark can be used in badges and statistics.

## License

The logo and the other brand assets in this folder are licensed under CC BY-ND 4.0 (share unchanged, with credit; no modified versions). The name and the logo are also trademarks, so they may not be used as the name or logo of another product or to suggest endorsement. See [LICENSE.md](LICENSE.md). The scripts (`build.mjs`, `package.json`) are covered by the repository's MIT License.

## Files

| Path | Use |
| --- | --- |
| `svg/logo-mark.svg` | Master. Adapts to light and dark mode with `prefers-color-scheme`; use in READMEs and web pages (as `<img>`, not inline). |
| `svg/logo-mark-light.svg` | Fixed colors for light backgrounds, where media queries are not supported (documents, slides, e-mail). |
| `svg/logo-mark-dark.svg` | Fixed colors for dark backgrounds. |
| `svg/logo-mark-mono-black.svg`, `svg/logo-mark-mono-white.svg` | One color, for print, stamps, embroidery and fax-like contexts. |
| `svg/logo-lockup.svg` | Horizontal lockup (mark plus the name in Space Grotesk, outlined). Adaptive like the master; use for README headers, site headers, slides and signatures. |
| `svg/logo-lockup-light.svg`, `svg/logo-lockup-dark.svg`, `svg/logo-lockup-mono-black.svg`, `svg/logo-lockup-mono-white.svg` | Fixed-color and mono versions of the lockup. |
| `png/logo-lockup-light-<width>.png`, `png/logo-lockup-dark-<width>.png` | Transparent lockup PNGs, 400, 800 and 1600 px wide. |
| `svg/logo-app-icon.svg` | Mark on a rounded dark square, for app icons and avatars. |
| `svg/social-preview.svg` | 1280 x 640 social image. |
| `png/logo-mark-light-<size>.png`, `png/logo-mark-dark-<size>.png` | Transparent PNGs at 64, 128, 256, 512 and 1024 px, for tools that cannot use SVG. |
| `png/favicon-16.png`, `-32.png`, `-48.png`, `png/apple-touch-icon.png`, `png/icon-192.png`, `png/icon-512.png` | Favicons, iOS home screen and web app manifest icons. |
| `png/social-preview.png` | GitHub repository social preview (Settings, Social preview) and link cards. |
| `favicon.ico` | 16, 32 and 48 px icon for browsers that request `/favicon.ico`. |
| `colors.json` | Brand colors, the single source for the build. |
| `build.mjs` | Generates every file above from one mark definition. |

Never edit the generated files by hand. Change `build.mjs` or `colors.json` and rebuild.

Only the SVGs in `svg/` are committed, because the README and the docs site reference them. The PNGs (`png/`) and `favicon.ico` are not in git: the `Brand Assets` workflow builds them on every change to this folder and publishes them as the `brand-assets` artifact of the run, together with the SVGs. The same workflow fails when the committed SVGs differ from what `build.mjs` produces, so rebuild and commit `svg/` after every change to `build.mjs` or `colors.json`.

## Colors

| Role | Light background | Dark background |
| --- | --- | --- |
| Ring (platform) | `#0b8f7e` | `#2fc2ad` |
| Progress arc (time) | `#f2a516` | `#f7b733` |
| Prompt (code) | `#14201f` | `#e8f0ef` |
| Dark surface | | `#0f1a19` |

The ring track is the ring color at 28 percent opacity.

## Usage rules

- Keep clear space around the mark equal to the width of the ring stroke (about 10 percent of the mark's width) on every side.
- Minimum size is 16 px. Below 24 px the mark still reads, but do not add text next to it.
- Use the adaptive master where the surface can be light or dark, the fixed variants everywhere else, and the mono variants only when color is not available.
- Do not stretch, rotate, recolor, outline or add effects to the mark. Do not change the arc length except to show a real percentage.
- The arc starts at 12 o'clock and runs clockwise. In a badge, set it to the real hours logged out of hours received.
- The name is written TimeForCode, one word with capital T, F and C.
- Keep the mark and the lockup separate: the mark alone is for small or square spaces (favicon, avatar, badge), the lockup for places with room for the name. Do not rearrange the lockup or set the name in another typeface.
- The name is Space Grotesk, bold for "Time" and "Code" and light for "For". The font is only a build dependency; the shipped files hold outlines.

## Rebuilding

Requires Node. From this folder:

```powershell
npm ci
npm run build
```

The build renders the PNGs with `@resvg/resvg-js`, so it needs no other tools. It writes the files into `svg/` and `png/` and overwrites `favicon.ico`. `png/` and `favicon.ico` are gitignored, so a local build leaves nothing to commit except `svg/`.

## Not yet included

- A stacked lockup (name below the mark).
- Using the mark in the front-end. That is application work and goes through the normal issue workflow.

The Reporting badge images show the mark on the left of the label. `SvgBadgeRenderer` holds its own copy of the mark in the dark-surface colors (the label is dark grey), drawn at 14 px. When the mark changes in `build.mjs`, update that copy too.

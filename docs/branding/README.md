# TimeForCode brand kit

The logo is a progress ring around a code prompt. The ring is the time: the amber arc is the share of donated hours that has been logged, the same measure as the progress badge. The prompt `>_` is the code. Because the ring can show a real percentage, the same mark can be used in badges and statistics.

## Files

| Path | Use |
| --- | --- |
| `svg/logo-mark.svg` | Master. Adapts to light and dark mode with `prefers-color-scheme`; use in READMEs and web pages (as `<img>`, not inline). |
| `svg/logo-mark-light.svg` | Fixed colors for light backgrounds, where media queries are not supported (documents, slides, e-mail). |
| `svg/logo-mark-dark.svg` | Fixed colors for dark backgrounds. |
| `svg/logo-mark-mono-black.svg`, `svg/logo-mark-mono-white.svg` | One color, for print, stamps, embroidery and fax-like contexts. |
| `svg/logo-app-icon.svg` | Mark on a rounded dark square, for app icons and avatars. |
| `svg/social-preview.svg` | 1280 x 640 social image. |
| `png/logo-mark-light-<size>.png`, `png/logo-mark-dark-<size>.png` | Transparent PNGs at 64, 128, 256, 512 and 1024 px, for tools that cannot use SVG. |
| `png/favicon-16.png`, `-32.png`, `-48.png`, `png/apple-touch-icon.png`, `png/icon-192.png`, `png/icon-512.png` | Favicons, iOS home screen and web app manifest icons. |
| `png/social-preview.png` | GitHub repository social preview (Settings, Social preview) and link cards. |
| `favicon.ico` | 16, 32 and 48 px icon for browsers that request `/favicon.ico`. |
| `colors.json` | Brand colors, the single source for the build. |
| `build.mjs` | Generates every file above from one mark definition. |

Never edit the generated files by hand. Change `build.mjs` or `colors.json` and rebuild.

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

## Rebuilding

Requires Node. From this folder:

```powershell
npm install
npm run build
```

The build renders the PNGs with `@resvg/resvg-js`, so it needs no other tools. It writes the files into `svg/` and `png/` and overwrites `favicon.ico`.

## Not yet included

- A wordmark or horizontal lockup (mark plus the name). It needs a chosen typeface, converted to outlines so it does not depend on installed fonts.
- Using the mark in the front-end and in the Reporting badge images. That is application work and goes through the normal issue workflow.

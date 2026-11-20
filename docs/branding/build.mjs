// Builds every logo asset from one mark definition.
// Usage: npm ci && npm run build   (in docs/branding)
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Resvg } from '@resvg/resvg-js';
import opentype from 'opentype.js';

const root = dirname(fileURLToPath(import.meta.url));
const colors = JSON.parse(readFileSync(join(root, 'colors.json'), 'utf8'));
const svgDir = join(root, 'svg');
const pngDir = join(root, 'png');
mkdirSync(svgDir, { recursive: true });
mkdirSync(pngDir, { recursive: true });

// The dash is computed from the circumference, because resvg ignores pathLength.
const CIRCUMFERENCE = 2 * Math.PI * 19;
const DASH = `${(CIRCUMFERENCE * 0.72).toFixed(2)} ${(CIRCUMFERENCE * 0.28).toFixed(2)}`;

// The mark: a progress ring (hours logged out of hours received) around a code prompt.
// Geometry is on a 48 x 48 grid; the progress arc covers 72 percent of the ring.
const geometry = (c, trackOpacity = 0.28) => `
  <circle cx="24" cy="24" r="19" stroke="${c.ring}" stroke-opacity="${trackOpacity}" stroke-width="5"/>
  <circle cx="24" cy="24" r="19" stroke="${c.progress}" stroke-width="5" stroke-dasharray="${DASH}" transform="rotate(-90 24 24)"/>
  <polyline points="16,17 24,24 16,31" stroke="${c.code}" stroke-width="4"/>
  <line x1="27" y1="32" x2="34" y2="32" stroke="${c.code}" stroke-width="4"/>`;

const open = (viewBox, extra = '') =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${viewBox}" fill="none" stroke-linecap="round" stroke-linejoin="round" role="img" aria-label="TimeForCode logo"${extra}>\n  <title>TimeForCode</title>`;

const mark = (c, trackOpacity) => `${open('0 0 48 48')}${geometry(c, trackOpacity)}\n</svg>\n`;

const adaptive = `${open('0 0 48 48')}
  <style>
    .track { stroke: ${colors.light.ring}; stroke-opacity: .28; }
    .time { stroke: ${colors.light.progress}; }
    .code { stroke: ${colors.light.code}; }
    @media (prefers-color-scheme: dark) {
      .track { stroke: ${colors.dark.ring}; }
      .time { stroke: ${colors.dark.progress}; }
      .code { stroke: ${colors.dark.code}; }
    }
  </style>
  <circle class="track" cx="24" cy="24" r="19" stroke-width="5"/>
  <circle class="time" cx="24" cy="24" r="19" stroke-width="5" stroke-dasharray="${DASH}" transform="rotate(-90 24 24)"/>
  <polyline class="code" points="16,17 24,24 16,31" stroke-width="4"/>
  <line class="code" x1="27" y1="32" x2="34" y2="32" stroke-width="4"/>
</svg>
`;

const mono = (color) => mark({ ring: color, progress: color, code: color }, 0.35);

// App icon: the dark-theme mark on a rounded dark square, with safe-zone padding.
const appIcon = `${open('0 0 96 96')}
  <rect width="96" height="96" rx="20" fill="${colors.surface.dark}" stroke="none"/>
  <g transform="translate(16 16) scale(1.3333)">${geometry(colors.dark)}</g>
</svg>
`;

// Social preview (GitHub repository social image is 1280 x 640): mark centered on the dark surface.
const social = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1280 640" fill="none" stroke-linecap="round" stroke-linejoin="round" role="img" aria-label="TimeForCode">
  <title>TimeForCode</title>
  <rect width="1280" height="640" fill="${colors.surface.dark}" stroke="none"/>
  <g transform="translate(480 160) scale(6.6667)">${geometry(colors.dark)}</g>
</svg>
`;

// Horizontal lockup: the mark followed by the name in Space Grotesk, converted to outlines so the
// logo does not depend on installed fonts. "Time" and "Code" are bold, "For" is light.
const fontFile = (weight) =>
  join(root, 'node_modules/@fontsource/space-grotesk/files', `space-grotesk-latin-${weight}-normal.woff`);
const loadFont = (weight) => {
  if (!existsSync(fontFile(weight))) {
    throw new Error(`Font file not found: ${fontFile(weight)}. Run "npm ci" in docs/branding first.`);
  }
  const b = readFileSync(fontFile(weight));
  return opentype.parse(b.buffer.slice(b.byteOffset, b.byteOffset + b.byteLength));
};
const bold = loadFont(700);
const light = loadFont(300);
const FONT_SIZE = 34;
const GAP = 14; // between the mark and the name
const wordRuns = [['Time', bold], ['For', light], ['Code', bold]];
const wordmarkPaths = (() => {
  let x = 48 + GAP;
  const baseline = 24 + (bold.tables.os2.sCapHeight / bold.unitsPerEm) * FONT_SIZE / 2;
  const d = wordRuns.map(([text, font]) => {
    const path = font.getPath(text, x, baseline, FONT_SIZE).toPathData(2);
    x += font.getAdvanceWidth(text, FONT_SIZE);
    return path;
  });
  return { d: d.join(' '), width: Math.ceil(x) };
})();
const lockupHeight = 48;
// Default display size of 72 px high, so a README renders it at a sensible size.
const lockupSize = ` width="${wordmarkPaths.width * 1.5}" height="${lockupHeight * 1.5}"`;
const lockup = (c, trackOpacity) =>
  `${open(`0 0 ${wordmarkPaths.width} ${lockupHeight}`, lockupSize)}${geometry(c, trackOpacity)}\n  <path d="${wordmarkPaths.d}" fill="${c.code}" stroke="none"/>\n</svg>\n`;
// Derives the adaptive lockup from the adaptive mark; fails loudly when the template no longer matches.
const mustReplace = (text, search, replacement) => {
  if (!text.includes(search)) throw new Error(`Adaptive lockup: template no longer contains "${search}"`);
  return text.replace(search, replacement);
};
let lockupAdaptive = adaptive;
lockupAdaptive = mustReplace(lockupAdaptive, 'viewBox="0 0 48 48"', `viewBox="0 0 ${wordmarkPaths.width} ${lockupHeight}"${lockupSize}`);
lockupAdaptive = mustReplace(lockupAdaptive, `.code { stroke: ${colors.light.code}; }`, `.code { stroke: ${colors.light.code}; }\n    .word { fill: ${colors.light.code}; }`);
lockupAdaptive = mustReplace(lockupAdaptive, `.code { stroke: ${colors.dark.code}; }`, `.code { stroke: ${colors.dark.code}; }\n      .word { fill: ${colors.dark.code}; }`);
lockupAdaptive = mustReplace(lockupAdaptive, '</svg>', `  <path class="word" d="${wordmarkPaths.d}" stroke="none"/>\n</svg>`);

const svgs = {
  'logo-lockup.svg': lockupAdaptive,
  'logo-lockup-light.svg': lockup(colors.light),
  'logo-lockup-dark.svg': lockup(colors.dark),
  'logo-lockup-mono-black.svg': lockup({ ring: colors.mono.black, progress: colors.mono.black, code: colors.mono.black }, 0.35),
  'logo-lockup-mono-white.svg': lockup({ ring: colors.mono.white, progress: colors.mono.white, code: colors.mono.white }, 0.35),
  'logo-mark.svg': adaptive,
  'logo-mark-light.svg': mark(colors.light),
  'logo-mark-dark.svg': mark(colors.dark),
  'logo-mark-mono-black.svg': mono(colors.mono.black),
  'logo-mark-mono-white.svg': mono(colors.mono.white),
  'logo-app-icon.svg': appIcon,
  'social-preview.svg': social,
};
for (const [name, content] of Object.entries(svgs)) writeFileSync(join(svgDir, name), content);

const render = (svg, width) =>
  new Resvg(svg, { fitTo: { mode: 'width', value: width } }).render().asPng();

// Transparent marks for light and dark backgrounds.
for (const size of [64, 128, 256, 512, 1024]) {
  writeFileSync(join(pngDir, `logo-mark-light-${size}.png`), render(svgs['logo-mark-light.svg'], size));
  writeFileSync(join(pngDir, `logo-mark-dark-${size}.png`), render(svgs['logo-mark-dark.svg'], size));
}

// Transparent lockups for light and dark backgrounds.
for (const width of [400, 800, 1600]) {
  writeFileSync(join(pngDir, `logo-lockup-light-${width}.png`), render(svgs['logo-lockup-light.svg'], width));
  writeFileSync(join(pngDir, `logo-lockup-dark-${width}.png`), render(svgs['logo-lockup-dark.svg'], width));
}

// App icons: web manifest, Apple touch icon, favicons.
const iconSizes = { 'favicon-16.png': 16, 'favicon-32.png': 32, 'favicon-48.png': 48, 'apple-touch-icon.png': 180, 'icon-192.png': 192, 'icon-512.png': 512 };
const icons = {};
for (const [name, size] of Object.entries(iconSizes)) {
  icons[name] = render(svgs['logo-app-icon.svg'], size);
  writeFileSync(join(pngDir, name), icons[name]);
}

writeFileSync(join(pngDir, 'social-preview.png'), render(svgs['social-preview.svg'], 1280));

// favicon.ico with embedded PNG images (16, 32, 48).
const parts = ['favicon-16.png', 'favicon-32.png', 'favicon-48.png'].map((n) => ({ size: Number(n.match(/\d+/)[0]), data: icons[n] }));
const header = Buffer.alloc(6);
header.writeUInt16LE(1, 2);
header.writeUInt16LE(parts.length, 4);
let offset = 6 + 16 * parts.length;
const dir = parts.map(({ size, data }) => {
  const e = Buffer.alloc(16);
  e.writeUInt8(size, 0);
  e.writeUInt8(size, 1);
  e.writeUInt16LE(1, 4);
  e.writeUInt16LE(32, 6);
  e.writeUInt32LE(data.length, 8);
  e.writeUInt32LE(offset, 12);
  offset += data.length;
  return e;
});
writeFileSync(join(root, 'favicon.ico'), Buffer.concat([header, ...dir, ...parts.map((p) => p.data)]));

console.log('Built logo assets in docs/branding');

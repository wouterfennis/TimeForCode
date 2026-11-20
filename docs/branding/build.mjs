// Builds every logo asset from one mark definition.
// Usage: npm install && npm run build   (in docs/branding)
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Resvg } from '@resvg/resvg-js';

const root = dirname(fileURLToPath(import.meta.url));
const colors = JSON.parse(readFileSync(join(root, 'colors.json'), 'utf8'));
const svgDir = join(root, 'svg');
const pngDir = join(root, 'png');
mkdirSync(svgDir, { recursive: true });
mkdirSync(pngDir, { recursive: true });

// The mark: a progress ring (hours logged out of hours received) around a code prompt.
// Geometry is on a 48 x 48 grid; the progress arc covers 72 percent of the ring.
const geometry = (c, trackOpacity = 0.28) => `
  <circle cx="24" cy="24" r="19" stroke="${c.ring}" stroke-opacity="${trackOpacity}" stroke-width="5"/>
  <circle cx="24" cy="24" r="19" stroke="${c.progress}" stroke-width="5" pathLength="100" stroke-dasharray="72 28" transform="rotate(-90 24 24)"/>
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
  <circle class="time" cx="24" cy="24" r="19" stroke-width="5" pathLength="100" stroke-dasharray="72 28" transform="rotate(-90 24 24)"/>
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

const svgs = {
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

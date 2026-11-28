// Renders every mockup page to PNG (desktop/mobile) or runs the checks.
//   node tools/render.mjs screenshots [prefix] -> writes ./screenshots/<page>.<viewport>.png (e.g. prefix v2/ for only the v2 pages)
//   node tools/render.mjs check         -> fails on axe violations (incl. colour contrast) and on skeletons
//                                          that do not line up with the content they stand in for
import { mkdir, readdir } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";
import AxeBuilder from "@axe-core/playwright";
import { startServer } from "./serve.mjs";

const mode = process.argv[2];
if (!["screenshots", "check"].includes(mode)) {
  console.error("Usage: render.mjs screenshots|check");
  process.exit(2);
}

const root = fileURLToPath(new URL("..", import.meta.url));
const outDir = `${root}screenshots`;
const htmlIn = async (dir) => (await readdir(`${root}${dir}`)).filter((f) => f.endsWith(".html")).map((f) => `${dir}${f.replace(".html", "")}`);
const only = process.argv[3] ?? "";
const pages = [...(await htmlIn("")), ...(await htmlIn("v2/"))].filter((p) => p.startsWith(only));
const viewports = { desktop: { width: 1280, height: 800 }, mobile: { width: 390, height: 844 } };

// Skeleton alignment: a skeleton block and the real element it stands in for share a data-frame name.
// Their left edges must match and their heights may differ a little (real text varies in length). Tops are not compared:
// a block only moves down because a block above it is taller, and that block is already reported.
const edgeTolerance = 8; // px, left edge
const heightTolerance = 32; // px
const framesIn = (page) => page.$$eval("[data-frame]", (els) => Object.fromEntries(els
  .filter((e) => e.getClientRects().length)
  .map((e) => { const r = e.getBoundingClientRect(); return [e.dataset.frame, { left: r.left, height: r.height }]; })));
const setState = (page, state) => page.evaluate((s) => (document.documentElement.dataset.state = s), state);

const server = await startServer(0);
const base = `http://localhost:${server.address().port}`;
// PW_CHANNEL=chrome uses the installed Chrome instead of Playwright's own browser download
const browser = await chromium.launch({ channel: process.env.PW_CHANNEL });
let problems = 0;

try {
  if (mode === "screenshots") await mkdir(outDir, { recursive: true });
  for (const [viewport, size] of Object.entries(viewports)) {
    const context = await browser.newContext({ viewport: size, colorScheme: "light", reducedMotion: "reduce" });
    for (const name of pages) {
      const page = await context.newPage();
      await page.goto(`${base}/${name}.html`, { waitUntil: "networkidle" });
      if (mode === "screenshots") {
        await page.addStyleTag({ content: ".mock-switch { display: none !important; }" }); // mockup-only panel
        await page.screenshot({ path: `${outDir}/${name.replace("/", "-")}.${viewport}.png`, fullPage: true });
      } else {
        if (viewport === "desktop") {
          const { violations } = await new AxeBuilder({ page }).exclude(".mock-switch").withTags(["wcag2a", "wcag2aa", "wcag22aa"]).analyze();
          for (const v of violations) {
            problems++;
            console.error(`[${name}] ${v.id}: ${v.help} (${v.nodes.length} node(s))`);
          }
        }
        const loaded = await page.evaluate(() => document.documentElement.dataset.state);
        if (loaded && (await page.$('[data-show~="loading"] [data-frame], [data-show~="loading"][data-frame]'))) {
          const real = await framesIn(page);
          await setState(page, "loading");
          const skeleton = await framesIn(page);
          await setState(page, loaded);
          for (const [frame, s] of Object.entries(skeleton)) {
            const r = real[frame];
            if (!r) { problems++; console.error(`[${name} ${viewport}] skeleton "${frame}" has no real counterpart`); continue; }
            const off = { left: s.left - r.left, height: s.height - r.height };
            if (Math.abs(off.left) > edgeTolerance || Math.abs(off.height) > heightTolerance) {
              problems++;
              console.error(`[${name} ${viewport}] skeleton "${frame}" is off by left ${off.left.toFixed(0)} px, height ${off.height.toFixed(0)} px`);
            }
          }
        }
      }
      await page.close();
    }
    await context.close();
  }
} finally {
  await browser.close();
  server.close();
}

if (mode === "check") {
  console.log(problems === 0 ? "No accessibility violations; skeletons line up." : `${problems} problem(s).`);
  process.exit(problems === 0 ? 0 : 1);
}
console.log(`Screenshots written to ${outDir}`);

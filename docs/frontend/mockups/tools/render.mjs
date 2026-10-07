// Renders every mockup page to PNG (desktop/mobile) or runs axe accessibility checks.
//   node tools/render.mjs screenshots   -> writes ./screenshots/<page>.<viewport>.png
//   node tools/render.mjs check         -> fails on axe violations (incl. colour contrast)
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
const pages = (await readdir(root)).filter((f) => f.endsWith(".html")).map((f) => f.replace(".html", ""));
const viewports = { desktop: { width: 1280, height: 800 }, mobile: { width: 390, height: 844 } };

const server = await startServer(0);
const base = `http://localhost:${server.address().port}`;
const browser = await chromium.launch();
let violations = 0;

try {
  if (mode === "screenshots") await mkdir(outDir, { recursive: true });
  {
    for (const [viewport, size] of Object.entries(viewports)) {
      const context = await browser.newContext({ viewport: size, colorScheme: "light", reducedMotion: "reduce" });
      for (const name of pages) {
        const page = await context.newPage();
        await page.goto(`${base}/${name}.html`);
        if (mode === "screenshots") {
          await page.screenshot({ path: `${outDir}/${name}.${viewport}.png`, fullPage: true });
        } else if (viewport === "desktop") {
          const { violations: found } = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag22aa"]).analyze();
          for (const v of found) {
            violations++;
            console.error(`[${name}] ${v.id}: ${v.help} (${v.nodes.length} node(s))`);
          }
        }
        await page.close();
      }
      await context.close();
    }
  }
} finally {
  await browser.close();
  server.close();
}

if (mode === "check") {
  console.log(violations === 0 ? "No accessibility violations." : `${violations} accessibility violation(s).`);
  process.exit(violations === 0 ? 0 : 1);
}
console.log(`Screenshots written to ${outDir}`);

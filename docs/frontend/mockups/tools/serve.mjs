// Minimal static file server for browsing the mockups: npm start -> http://localhost:4173
import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname, join, normalize } from "node:path";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("..", import.meta.url));
const types = { ".html": "text/html", ".css": "text/css", ".js": "text/javascript", ".png": "image/png" };

export function startServer(port = 4173) {
  const server = createServer(async (req, res) => {
    const path = normalize(decodeURIComponent(new URL(req.url, "http://x").pathname)).replace(/^([/\\])+/, "");
    try {
      const file = join(root, path === "" ? "index.html" : path);
      if (!file.startsWith(root)) throw new Error("outside root");
      res.writeHead(200, { "content-type": types[extname(file)] ?? "application/octet-stream" });
      res.end(await readFile(file));
    } catch {
      res.writeHead(404).end("Not found");
    }
  });
  return new Promise((resolve) => server.listen(port, () => resolve(server)));
}

if (import.meta.url === `file://${process.argv[1].replace(/\\/g, "/")}` || process.argv[1]?.endsWith("serve.mjs")) {
  await startServer();
  console.log("Mockups at http://localhost:4173");
}

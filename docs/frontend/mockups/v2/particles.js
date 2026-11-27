// Moodboard particles (PS5-like): soft dots at different depths in the ambient layer.
// "react": they only move when the visitor does (pointer, scroll, focusing a tile) and then settle.
// "drift": they also float slowly on their own; paused when the tab is hidden. Reduced motion: drawn once, no movement.
(() => {
  const root = document.documentElement;
  const ambient = document.querySelector(".ambient");
  const canvas = Object.assign(document.createElement("canvas"), { className: "particles" });
  ambient.prepend(canvas);
  const ctx = canvas.getContext("2d");
  const reduce = matchMedia("(prefers-reduced-motion: reduce)");
  const parts = Array.from({ length: 110 }, () => ({
    x: Math.random(), y: Math.random(), z: 0.15 + Math.random() * 0.85, r: 0.6 + Math.random() * 1.8,
    a: 0.2 + Math.random() * 0.5, vx: (Math.random() - 0.5) * 0.000006, vy: -(0.000004 + Math.random() * 0.00001),
  }));
  const hex = (h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16));
  const cur = { ox: 0, oy: 0, s: 0, c: hex("#66c0f4") };
  const target = { ox: 0, oy: 0, s: 0, c: [...cur.c] };

  // One soft glow, re-tinted when the colour changes, drawn at every particle's size
  const sprite = document.createElement("canvas");
  sprite.width = sprite.height = 64;
  const paintSprite = () => {
    const g = sprite.getContext("2d");
    const [r, gr, b] = cur.c.map(Math.round);
    const grad = g.createRadialGradient(32, 32, 0, 32, 32, 32);
    grad.addColorStop(0, `rgb(${r} ${gr} ${b} / 1)`);
    grad.addColorStop(0.25, `rgb(${r} ${gr} ${b} / 0.5)`);
    grad.addColorStop(1, `rgb(${r} ${gr} ${b} / 0)`);
    g.clearRect(0, 0, 64, 64);
    g.fillStyle = grad;
    g.fillRect(0, 0, 64, 64);
  };

  let W = 0, H = 0, dpr = 1;
  const resize = () => { dpr = Math.min(2, devicePixelRatio || 1); W = canvas.width = innerWidth * dpr; H = canvas.height = innerHeight * dpr; };
  const mod = (n, m) => ((n % m) + m) % m;
  // Near particles (high z) move more than far ones: that gives the depth
  const draw = () => {
    ctx.clearRect(0, 0, W, H);
    for (const p of parts) {
      const x = mod(p.x * W + cur.ox * p.z * dpr, W);
      const y = mod(p.y * H + (cur.oy - cur.s) * p.z * dpr, H);
      const size = p.r * p.z * 14 * dpr;
      ctx.globalAlpha = p.a * (0.35 + p.z * 0.65);
      ctx.drawImage(sprite, x - size / 2, y - size / 2, size, size);
    }
  };

  let running = false, last = 0;
  const frame = (t) => {
    const dt = Math.min(64, t - (last || t));
    last = t;
    const mode = root.dataset.particles, still = reduce.matches;
    if (mode === "drift" && !still) for (const p of parts) { p.x = mod(p.x + p.vx * dt, 1); p.y = mod(p.y + p.vy * dt, 1); }
    const k = still ? 1 : 1 - Math.pow(0.94, dt / 16); // slowing ease towards the target (secondary motion)
    let busy = false;
    for (const key of ["ox", "oy", "s"]) { cur[key] += (target[key] - cur[key]) * k; busy ||= Math.abs(target[key] - cur[key]) > 0.2; }
    cur.c = cur.c.map((v, i) => v + (target.c[i] - v) * k * 0.6);
    busy ||= cur.c.some((v, i) => Math.abs(target.c[i] - v) > 0.5);
    paintSprite();
    draw();
    if (mode !== "none" && !document.hidden && (busy || (mode === "drift" && !still))) requestAnimationFrame(frame);
    else { running = false; last = 0; }
  };
  const kick = () => { if (!running && root.dataset.particles !== "none") { running = true; requestAnimationFrame(frame); } };

  resize(); paintSprite(); kick();
  addEventListener("resize", () => { resize(); kick(); });
  document.addEventListener("visibilitychange", kick);
  document.addEventListener("pointermove", (e) => { target.ox = (e.clientX / innerWidth - 0.5) * -60; target.oy = (e.clientY / innerHeight - 0.5) * -40; kick(); });
  addEventListener("scroll", () => { target.s = scrollY * 0.35; kick(); }, { passive: true });

  // Tile hover/focus: particles take the project's colour
  const tint = (e) => {
    const c = e.target.closest?.(".tile")?.style.getPropertyValue("--tile-colour").trim();
    if (c) { target.c = hex(c); kick(); }
  };
  document.addEventListener("mouseover", tint);
  document.addEventListener("focusin", tint);
  document.getElementById("lab").addEventListener("click", () => setTimeout(kick));

  // Holographic texture: the sheen follows the pointer across a card
  document.addEventListener("pointermove", (e) => {
    const t = root.dataset.texture === "holo" && e.target.closest?.(".tile, .panel, .stat");
    if (!t) return;
    const r = t.getBoundingClientRect();
    t.style.setProperty("--hx", `${((e.clientX - r.left) / r.width) * 100}%`);
  });
})();

// Shared sample data and helpers for the v2 page mockups. Sample data only: real repositories so the
// Open Graph images load; "colour" stands in for a colour the app could take from the project image.
export const projects = [
  { id: "1", repo: "dotnet/aspire", need: "Dashboard accessibility and hosting docs", tags: ["C#", "Docs"], pledged: 120, goal: 200, colour: "#7b61ff" },
  { id: "2", repo: "withastro/astro", need: "Help triaging issues in the i18n router", tags: ["TypeScript", "Triage"], pledged: 45, goal: 60, colour: "#ff5d01" },
  { id: "3", repo: "excalidraw/excalidraw", need: "Keyboard support for the shape picker", tags: ["TypeScript", "A11y"], pledged: 18, goal: 80, colour: "#2f9e44" },
  { id: "4", repo: "jellyfin/jellyfin", need: "Performance work on library scans", tags: ["C#", "Backend"], pledged: 64, goal: 100, colour: "#00a4dc" },
  { id: "5", repo: "mastodon/mastodon", need: "Security review of media uploads", tags: ["Ruby", "Security"], pledged: 30, goal: 40, colour: "#d6336c" },
];

export const art = (repo) => `https://opengraph.githubassets.com/1/${repo}`;

// Project tile; data-testids match the E2E tests (page-specs.md)
export const tile = (p, { compact = false } = {}) => {
  const pct = Math.round((p.pledged / p.goal) * 100);
  return `<article class="tile" data-testid="project-tile" data-id="${p.id}" style="--tile-colour: ${p.colour}; --tile-art: url(${art(p.repo)})">
    <div class="tile-media"><img src="${art(p.repo)}" alt="" loading="lazy" onerror="this.remove()"><span class="badge">${pct}% pledged</span></div>
    <div class="tile-body">
      <h3><a href="project-detail.html?id=${p.id}" data-testid="project-link">${p.repo}</a></h3>
      ${compact ? "" : `<p>${p.need}</p><div class="tags">${p.tags.map((t) => `<span class="tag">${t}</span>`).join("")}</div>`}
      <div class="hours"><span><strong>${p.pledged}</strong> of ${p.goal} h</span><span>${p.goal - p.pledged} h to go</span></div>
      <div class="progress" aria-hidden="true"><span style="width: ${pct}%"></span></div>
    </div></article>`;
};

// Spotlight: two background layers; the art of the hovered or focused tile slides in and settles
export const spotlight = (root, items, onShow = () => {}) => {
  const layers = [...root.querySelectorAll(".spotlight-bg")];
  const tiles = [...root.querySelectorAll(".tile")];
  let front = 0, current = -1;
  const show = (i) => {
    if (i === current) return;
    const dir = i > current ? 1 : -1;
    current = i;
    const back = layers[1 - front], old = layers[front];
    back.style.transition = "none"; back.style.setProperty("--from", `${dir * 8}%`); void back.offsetWidth; back.style.transition = "";
    old.style.setProperty("--from", `${-dir * 8}%`);
    back.style.backgroundImage = `url(${art(items[i].repo)})`;
    back.classList.add("is-visible"); old.classList.remove("is-visible"); front = 1 - front;
    onShow(items[i]);
  };
  tiles.forEach((el, i) => ["mouseenter", "focusin"].forEach((ev) => el.addEventListener(ev, () => show(i))));
  if (items.length) show(0);
};

// Mockup-only state switcher (fixed bottom right): sets data attributes on <html>
export const stateSwitcher = (groups) => {
  const box = document.createElement("details");
  box.className = "mock-switch"; box.open = true;
  box.innerHTML = `<summary>Mockup state</summary>` + groups.map(([attr, label, opts]) =>
    `<div class="toolbar" role="group" aria-label="${label}" data-attr="${attr}"><span class="label">${label}</span>` +
    opts.map(([key, name]) => `<button class="btn" type="button" data-key="${key}" aria-pressed="${document.documentElement.dataset[attr] === key}">${name}</button>`).join("") + "</div>").join("");
  box.addEventListener("click", (e) => {
    const btn = e.target.closest("button"); if (!btn) return;
    const group = btn.closest("[data-attr]");
    group.querySelectorAll("button").forEach((b) => b.setAttribute("aria-pressed", b === btn));
    document.documentElement.dataset[group.dataset.attr] = btn.dataset.key;
  });
  document.body.append(box);
};

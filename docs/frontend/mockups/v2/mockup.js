// Shared sample data and helpers for the v2 page mockups.
// Fields follow the Donation API (ProjectSummaryResponse / ProjectResponse); the numbers are rough samples.
// Real repositories, so GitHub's Open Graph images load.

/** GitHub's language colours; the card tint comes from the project's primary language. */
export const languageColours = {
  "C#": "#178600", TypeScript: "#3178c6", JavaScript: "#f1e05a", Ruby: "#701516", Python: "#3572a5",
  "C++": "#f34b7d", PHP: "#4f5d95", Go: "#00add8",
};

const p = (id, fullName, description, language, stargazersCount, forksCount, openIssuesCount, topics, license) => {
  const [ownerLogin] = fullName.split("/");
  return { id, fullName, description, language, stargazersCount, forksCount, openIssuesCount, topics, license, ownerLogin,
    githubUrl: `https://github.com/${fullName}`, ownerAvatarUrl: `https://github.com/${ownerLogin}.png`, colour: languageColours[language] ?? "#66c0f4" };
};

/** Sample projects, as the list endpoint would return them. */
export const projects = [
  p("1", "dotnet/aspire", "Tools, templates and packages to accelerate building observable, production-ready apps.", "C#", 4800, 650, 900, ["dotnet", "cloud-native", "observability"], "MIT"),
  p("2", "withastro/astro", "The web framework for content-driven websites.", "TypeScript", 51000, 2800, 230, ["static-site-generator", "ssr", "islands"], "MIT"),
  p("3", "excalidraw/excalidraw", "Virtual whiteboard for sketching hand-drawn like diagrams.", "TypeScript", 98000, 9500, 1500, ["whiteboard", "diagrams", "collaboration"], "MIT"),
  p("4", "jellyfin/jellyfin", "The free software media system.", "C#", 39000, 3400, 400, ["media-server", "streaming"], "GPL-2.0"),
  p("5", "mastodon/mastodon", "Your self-hosted, globally interconnected microblogging community.", "Ruby", 48000, 7000, 3000, ["activitypub", "fediverse", "social-network"], "AGPL-3.0"),
  p("6", "home-assistant/core", "Open source home automation that puts local control and privacy first.", "Python", 78000, 33000, 2700, ["home-automation", "iot"], "Apache-2.0"),
  p("7", "godotengine/godot", "Multi-platform 2D and 3D game engine.", "C++", 98000, 21000, 13000, ["game-engine", "gamedev"], "MIT"),
  p("8", "immich-app/immich", "High performance self-hosted photo and video management solution.", "TypeScript", 60000, 3200, 600, ["photos", "self-hosted", "backup"], "AGPL-3.0"),
  p("9", "bitwarden/server", "Bitwarden infrastructure/backend (API, database, Docker, etc).", "C#", 16000, 1300, 100, ["password-manager", "security"], "AGPL-3.0"),
  p("10", "nextcloud/server", "A safe home for all your data.", "PHP", 29000, 4300, 3500, ["file-sharing", "self-hosted", "collaboration"], "AGPL-3.0"),
  p("11", "sveltejs/svelte", "Web development for the rest of us.", "JavaScript", 82000, 4300, 900, ["ui", "compiler", "reactivity"], "MIT"),
  p("12", "ollama/ollama", "Get up and running with large language models locally.", "Go", 140000, 12000, 1800, ["llm", "local-ai"], "MIT"),
];

/** GitHub's generated Open Graph image (interim project image, issue #252). */
export const art = (fullName) => `https://opengraph.githubassets.com/1/${fullName}`;

/** Compact number, e.g. 98000 -> "98k". */
export const short = (n) => (n >= 1000 ? `${Math.round(n / 100) / 10}k`.replace(".0k", "k") : String(n));

/** Image fallback chain from #213: Open Graph image, then the owner avatar, then a plain tile. */
const fallback = (avatar) => `onerror="if (this.dataset.fallback) { this.remove() } else { this.dataset.fallback = '1'; this.src = '${avatar}'; this.classList.add('is-avatar') }"`;

/** Project tile; data-testids match the E2E tests (page-specs.md). */
export const tile = (x, { compact = false } = {}) => `<article class="tile" data-testid="project-tile" data-id="${x.id}" style="--tile-colour: ${x.colour}; --tile-art: url(${art(x.fullName)})">
    <div class="tile-media"><img src="${art(x.fullName)}" alt="" loading="lazy" width="1200" height="600" ${fallback(x.ownerAvatarUrl)}></div>
    <div class="tile-body">
      <h3><a href="project-detail.html?id=${x.id}" data-testid="project-link">${x.fullName}</a></h3>
      ${compact ? "" : `<p>${x.description ?? ""}</p>`}
      <ul class="meta" aria-label="Repository facts">
        ${x.language ? `<li><span class="lang-dot" style="background: ${x.colour}" aria-hidden="true"></span>${x.language}</li>` : ""}
        <li><span aria-hidden="true">★</span> ${short(x.stargazersCount)}<span class="visually-hidden"> stars</span></li>
        ${compact ? "" : `<li>${short(x.openIssuesCount)} open issues</li>`}
      </ul>
      ${compact ? "" : `<a class="gh-link" href="${x.githubUrl}" target="_blank" rel="noopener">GitHub<span class="visually-hidden"> repository of ${x.fullName}</span> <span aria-hidden="true">↗</span><span class="visually-hidden"> (opens in a new tab)</span></a>`}
    </div></article>`;

/** Spotlight: two background layers; the art of the hovered or focused tile slides in and settles. */
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
    back.style.backgroundImage = `url(${art(items[i].fullName)})`;
    back.classList.add("is-visible"); old.classList.remove("is-visible"); front = 1 - front;
    onShow(items[i]);
  };
  tiles.forEach((el, i) => ["mouseenter", "focusin"].forEach((ev) => el.addEventListener(ev, () => show(i))));
  if (items.length) show(0);
};

/** Mockup-only state switcher (fixed bottom right): sets data attributes on <html>. */
export const stateSwitcher = (groups, onChange = () => {}) => {
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
    onChange(group.dataset.attr, btn.dataset.key);
  });
  document.body.append(box);
};

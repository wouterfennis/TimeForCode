// Renders the shared header and footer so page files only contain page content.
// Usage: <body data-nav="projects" data-user="visitor|user|admin"> with #app-header and #app-footer placeholders.
const nav = document.body.dataset.nav ?? "";
const user = document.body.dataset.user ?? "visitor";
const link = (href, key, text) => `<a href="${href}"${nav === key ? ' aria-current="page"' : ""}>${text}</a>`;
const auth = user === "visitor"
  ? `<a class="btn primary" href="#">Sign in with GitHub</a>`
  : `<span data-testid="user-name">Ada Lovelace</span> <a class="btn" href="#" data-testid="logout-link">Sign out</a>`;
document.getElementById("app-header").outerHTML = `
  <a class="skip" href="#main">Skip to content</a>
  ${user === "admin" ? `<div class="admin-bar"><div class="container"><strong>Admin</strong>
    <a href="admin-donor-organizations.html">Donor organizations</a></div></div>` : ""}
  <header class="app"><div class="container">
    <a class="brand" href="index.html">Time<b>For</b>Code</a>
    <nav class="primary" aria-label="Primary">
      ${link("projects.html", "projects", "Projects")}
      ${user !== "visitor" ? link("profile.html", "profile", "Profile") : ""}
    </nav>${auth}
  </div></header>`;
document.getElementById("app-footer").outerHTML = `
  <footer class="app"><div class="container">
    <a href="#">GitHub repository</a><a href="#">Documentation</a><a href="#">Code of conduct</a>
    <a href="#">Admin sign-in</a>
  </div></footer>`;

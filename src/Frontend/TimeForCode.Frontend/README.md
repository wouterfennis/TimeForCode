# TimeForCode front-end

React single-page application that replaces the deprecated Blazor Website ([ADR-012](../../../docs/reference/adr/0012-react-spa-frontend.md)). Stack, folder layout and testing strategy: [docs/frontend/architecture.md](../../../docs/frontend/architecture.md).

## Prerequisites

Node 24, as pinned in `.nvmrc` (`nvm use` picks it up). `npm install` refuses other major versions (`engine-strict`).

## Scripts

Run from this folder:

| Command | What it does |
| --- | --- |
| `npm ci` | Install the exact dependencies from `package-lock.json` |
| `npm run dev` | Start the Vite dev server |
| `npm run lint` | Lint with oxlint; warnings fail the check |
| `npm run typecheck` | Type-check with `tsc` in strict mode |
| `npm test` | Run the Vitest component tests once (`npm run test:watch` to watch) |
| `npm run build` | Type-check and build to `dist/` |

The pull request workflow runs `npm ci`, lint, type-check, test and build.

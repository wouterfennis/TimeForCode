# TimeForCode front-end

React single-page application for TimeForCode ([ADR-012](../../../docs/reference/adr/0012-react-spa-frontend.md)). Stack, folder layout and testing strategy: [docs/frontend/architecture.md](../../../docs/frontend/architecture.md).

## Prerequisites

Node 24, as pinned in `.nvmrc` (`nvm use` picks it up). `npm install` refuses other major versions (`engine-strict`).

## Scripts

Run from this folder:

| Command | What it does |
| --- | --- |
| `npm ci` | Install the exact dependencies from `package-lock.json` |
| `npm run dev` | Start the Vite dev server; it forwards `/api/donation/*` to the Donation API on `localhost:8082` (see [Dev server against the local stack](../../../docs/frontend/architecture.md#dev-server-against-the-local-stack)) |
| `npm run generate:api` | Regenerate `src/api/donation.d.ts` from the Donation OpenAPI snapshot (needs network for `npx` the first time) |
| `npm run check:api` | Regenerate and fail if the committed types differ (CI runs this) |
| `npm run verify` | Run every check in order: `check:api`, lint, type-check, tests and build |
| `npm run lint` | Lint with oxlint; warnings fail the check |
| `npm run typecheck` | Type-check with `tsc` in strict mode |
| `npm test` | Run the Vitest component tests once (`npm run test:watch` to watch) |
| `npm run test:coverage` | Run the tests with coverage; writes `coverage/lcov.info` for SonarCloud |
| `npm run build` | Type-check and build to `dist/` |

The pull request workflow runs `npm ci --ignore-scripts`, lint, type-check, `test:coverage` and build.

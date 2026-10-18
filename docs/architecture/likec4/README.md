# TimeForCode architecture model

[LikeC4](https://likec4.dev) model of the TimeForCode architecture. It is the single source for the context, building block, runtime and deployment views and complements the arc42 chapters in [`../arc42/`](../arc42/README.md).

Published site: <https://wouterfennis.github.io/TimeForCode/architecture-model/> (part of the documentation site at <https://wouterfennis.github.io/TimeForCode/>)

## Files

| File | Content |
| --- | --- |
| `specification.c4` | Element kinds and deployment node kinds |
| `model.c4` | Actors, external systems, bounded contexts (Authorization, Donation, Shared) and their layers |
| `deployment.c4` | Production (Azure, from `deploy/`) and local (`docker-compose.yaml`) deployments |
| `views.c4` | System context, bounded context, dynamic and deployment views |

The deprecated Website project is not modelled.

## Commands

Requires Node.js 22 or newer. Run from this folder.

```bash
npm ci               # install the pinned likec4
npm run validate     # fail on an invalid model
npm run serve        # live preview at http://localhost:5173
npm run build        # static site in dist/ (gitignored)
```

## Views

| View | Id | arc42 chapter |
| --- | --- | --- |
| System context | `index` | 03 |
| Bounded contexts | `containers` | 05 |
| Authorization bounded context | `authorization` | 05 |
| Donation bounded context | `donation` | 05 |
| Shared library | `shared` | 05 |
| GitHub login (dynamic) | `githubLogin` | 06 |
| Log a time contribution (dynamic) | `logContribution` | 06 |
| Production deployment | `production` | 07 |
| Local development | `local` | 07 |

## Keeping it in sync

Update the model when the building blocks, the runtime flows or the resources in `deploy/` change. The `Documentation Site` workflow validates the model on pull requests. On push to `main` it publishes the Jekyll docs at the site root and this model at `/architecture-model/` to GitHub Pages.

Repository setup (one time, manual): Settings, Pages, Source set to "GitHub Actions".

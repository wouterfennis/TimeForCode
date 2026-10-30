# TimeForCode architecture model

[LikeC4](https://likec4.dev) model of the TimeForCode architecture. It is the single source for the context, building block, runtime and deployment views and complements the arc42 chapters in [`../arc42/`](../arc42/README.md).

Published site: <https://wouterfennis.github.io/TimeForCode/architecture-model/> (part of the documentation site at <https://wouterfennis.github.io/TimeForCode/>)

## Files

| File | Content |
| --- | --- |
| `specification.c4` | Element kinds, tags and deployment node kinds |
| `model.c4` | Actors, external systems, the front-end, the bounded contexts (Authorization, Donation, Shared) with their layers and components, and the delivery pipeline |
| `deployment.c4` | Production (Azure, from `deploy/`), local (`docker-compose.yaml`) and the planned target deployment |
| `views.c4` | Static views per level, the delivery view, dynamic views of the runtime scenarios and the deployment views |

The deprecated Website project is not modelled.

## Commands

Requires Node.js 22 or newer. Run from this folder.

```bash
npm ci               # install the pinned likec4
npm run validate     # fail on an invalid model
npm run serve        # live preview at http://localhost:5173
npm run build        # static site in dist/ (gitignored)
```

## Levels and conventions

| C4 level | Modelled as | Where |
| --- | --- | --- |
| 1 System context | actors, `timeforcode`, external systems | `model.c4` |
| 2 Containers | `frontend`, `bff` (planned), `authorization`, `donation`, `shared` | `model.c4` |
| 3 Components | the clean-architecture layers of each bounded context (`layer`), each with its components: controllers, handler groups, services, aggregates, repositories | `model.c4` |

- A `layer` is a project of a bounded context (`TimeForCode.<Context>.<Layer>`); its components are the classes or groups of classes that matter for the flows. Components of the Application layer reach Infrastructure through interfaces; the relationship text names the interface.
- Tag `#planned` marks what is proposed or not built yet (drawn dashed where styled), `#testOnly` what exists only for tests and local runs, `#external` systems outside the repository.
- Dynamic views carry the runtime scenario number of arc42 chapter 6 in their title.
- Test projects are not modelled; the Authorization and Donation `Api.Client` projects are generated from the OpenAPI documents and used only by the specification tests.

## Views

| View | Id | arc42 chapter |
| --- | --- | --- |
| System context | `index` | 03 |
| Containers | `containers` | 03, 05 |
| Front-end | `frontend` | 05 |
| Authorization bounded context | `authorization` | 05 |
| Donation bounded context | `donation` | 05 |
| Shared library | `shared` | 05 |
| Authorization: API, Application, Infrastructure layer | `authorizationApi`, `authorizationApplication`, `authorizationInfrastructure` | 05 |
| Donation: API, Application, Domain, Infrastructure layer | `donationApi`, `donationApplication`, `donationDomain`, `donationInfrastructure` | 05 |
| Delivery pipeline | `delivery` | 07 |
| Scenario 1: GitHub login | `githubLogin` | 06 |
| Scenario 2: Token refresh | `tokenRefresh` | 06 |
| Scenario 2b: Delete account | `deleteAccount` | 06 |
| Administrator passkey sign-in | `adminSignIn` | 06, ADR-011 |
| Scenario 3: Register a project | `registerProject` | 06 |
| Scenario 3b: Review a project | `reviewProject` | 06 |
| Scenario 3c: Search active projects | `searchProjects` | 06 |
| Scenario 3d: Pledge hours | `pledgeDonation` | 06 |
| Log a time contribution | `logContribution` | 06 |
| Scenario 3d1: Correct or delete a contribution | `correctOrDeleteContribution` | 06 |
| Scenario 3d2: Maintainer projects and donations | `maintainerOverview` | 06 |
| Scenario 3e: Contributor profile | `contributorProfile` | 06 |
| Production deployment | `production` | 07 |
| Local development | `local` | 07 |
| Target production deployment (planned) | `target` | 07 |

## Keeping it in sync

Update the model when the building blocks, the runtime flows or the resources in `deploy/` change. The `Documentation Site` workflow validates the model on pull requests. On push to `main` it publishes the Jekyll docs at the site root and this model at `/architecture-model/` to GitHub Pages.

Repository setup (one time, manual): Settings, Pages, Source set to "GitHub Actions".

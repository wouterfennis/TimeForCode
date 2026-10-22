# Lessons learned

Non-obvious knowledge from past sessions. Read and update through skill `lessons-learned`; entry format is defined there.

## Workflow and gates

### Check the approval markers before starting a phase

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** `/write-feature` needs `APPROVED: plan` and `/implement-issue` needs `APPROVED: feature` as the first line of a human comment on the issue.
- **Why:** both phases stop at the gate; the jq `test("\\s*$")` variant fails to parse in `gh --jq`, so use `test("^APPROVED: (plan|feature)"; "m")`.
- **How to apply:** run the marker check first and tell the user which marker to post instead of starting work.

### Ask about scope-changing decisions up front

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** A change that alters existing behaviour (lifecycle status, a removed endpoint) deserves one AskUserQuestion before coding.
- **How to apply:** grep for other consumers of an endpoint before removing it, and surface any conflict with the issue's criteria in the question.

## Tooling and environment

### Shell quirks on this machine

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** No Python is installed; large heredocs with apostrophes inside a single Bash call can fail to parse and run nothing; `sleep` followed by a command is blocked.
- **Why:** a failed parse silently skips the whole command, which looks like a no-op.
- **How to apply:** create files with the Write tool, keep Bash commands short, wait with `run_in_background` or Monitor.

### `dotnet format ./TimeForCode.sln` churns unrelated files

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** The solution-wide format modifies files in other modules, which pollutes the PR.
- **How to apply:** after formatting, run `git status --short` and `git checkout --` every path outside the issue's scope.

### Read guard and Node engines (front-end work)

- **Date / issue:** 2026-10-15 / #110
- **Lesson:** The read-guard hook blocks Bash reads (`grep`, `cat`, `ls`...) of `node_modules` and lockfiles, except a dependency's own `package.json`; append `# allow-excluded` when a wider read is intended. Local Node is 24.12, so packages needing 24.15 or later fail with `engine-strict`; pick a release that supports every Node 24.

### The Podman guard also matches the compose file name

- **Date / issue:** 2026-10-21 / #111
- **Lesson:** `guard-bash.ps1` denies any shell command with an unquoted `docker-compose` word, so `git diff -- docker-compose*` or `Get-Content docker-compose.yaml` is refused too.
- **How to apply:** read the compose file with Read or Grep, or quote the path; `podman build -f Dockerfile.frontend` is fine.

### The local Mongo volume keeps old data shapes

- **Date / issue:** 2026-10-21 / #111
- **Lesson:** After a breaking data change (ADR-014), old documents stay in the local `mongo` volume; the smoke test then fails with a 500 (here: `ux_refresh_token_hash` could not be built because old `RefreshToken` documents had no `TokenHash`).
- **How to apply:** read the API container log (`podman logs timeforcode-authorization-api`), then delete only the old-shape documents with `podman exec mongo mongosh ...` and rerun.

## Front-end

### `nginx -t` runs at image build time, without the compose network

- **Date / issue:** 2026-10-21 / #111
- **Lesson:** `Dockerfile.frontend` runs `nginx -t`, which resolves a literal `proxy_pass` host such as `donation-api` and fails with "host not found in upstream" during the build. Server-level `add_header` relies on `add_header_inherit merge` (nginx 1.29.3 or later).
- **How to apply:** for the first proxy location (#112), use an `upstream` with `server donation-api:8080 resolve;` plus a `resolver`, or a variable in `proxy_pass`, and keep the smoke test's header checks green.

## Donation module

### Project status values are persisted as integers

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** `ProjectStatus` is stored as its numeric value (Active=0, Archived=1, Draft=2, PendingApproval=3); never renumber or reorder.
- **Why:** old `Published` documents are 0 and would silently change meaning.
- **How to apply:** only append new members with explicit numbers.

### Admin tokens use scope `admin`, user tokens scope `user`

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** The Authorization API issues admin JWTs with `scope=admin` and `role=admin` (sub `admin`). Donation policies: `ApiUser` (scope user), `ApiAdmin` (scope admin).
- **Why:** an admin token fails `ApiUser`, so admin and maintainer endpoints are naturally disjoint.
- **How to apply:** use `ApiAdmin` for admin-only endpoints; spec step "The user has an administrator access token" builds such a token.

### NSwag client and Swagger snapshot follow the controller

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** `TimeForCode.Donation.Api.json` is regenerated on build and drives the generated client used by the Specifications; `SwaggerTests` then fails until the verified snapshot is replaced after review (skill `swagger-snapshot-review`).
- **Why:** removing or renaming an endpoint breaks every consumer at compile time.
- **How to apply:** build the whole solution after any controller change (Api project first, so the JSON is regenerated); accept the snapshot only after reading the diff. NSwag emits positional parameters, so adding a query param removes the old overload and breaks its callers; grep `GetXxxAsync(` first.

### Reqnroll catalog fakes need distinct ids and `BsonDocument` needs `ToJson()` in assertions

- **Date / issue:** 2026-10-13 / #76
- **Lesson:** `ProjectBuilder.BuildActive()` and the lifecycle project share `Constants.TestProjectId`, so an "is not included" check on that id fails; use `ProjectBuilder.IdFor(n)`. FluentAssertions treats `BsonDocument` as a collection of elements, so compare `ToJson()` strings.
- **Why:** both cost a failed run each.
- **How to apply:** give each project in an in-memory spec catalog its own id; render Mongo filters with `RenderArgs<T>` (driver 3.x) and compare JSON.

### Declare input limits as attributes, not in controller code

- **Date / issue:** 2026-10-13 / #76, PR #98
- **Lesson:** `Range`, `StringLength`, `MaxLength`, `RegularExpression` and `[Url]` on action parameters or request properties appear in the OpenAPI document and are enforced by `[ApiController]` (400 validation problem details). `[EmailAddress]` is enforced but not emitted as `format: email` (shared `OpenApiTransformers.AddEmailFormat` adds it); `MaxLength` cannot be put on a `Uri` property. `[EmailAddress]` rejects `""`, which the old FluentValidation rule allowed.
- **Why:** manual `if (pageNumber < 1)` checks left the schema silent, and the donor-organization list answered 500 for huge page numbers.
- **How to apply:** put shared limits in the module's `ApiConstraints`, change the Swagger snapshot only after reading the diff, and give a spec scenario to every behaviour change (for example malformed ids now 400, not 404).

### Spec step classes can be split with `partial`

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** Reqnroll step classes keep state in private fields (for example `_exception`), so shared Then-steps only see state in the same class. Split large step classes as `partial class` files (`ProjectSteps.Lifecycle.cs`).
- **Why:** a second step class would not see the first one's captured API error.
- **How to apply:** extend the existing partial class rather than adding a new binding class for the same feature area.

### Live scenarios sign in through the real login and need the stack's admin secret

- **Date / issue:** 2026-10-22 / #137
- **Lesson:** a `@live` Donation scenario gets a user token by replaying the smoke-test login (Auth login, mock IdP confirm form, callback) and reads the `AccessToken` cookie, whose JSON is PascalCase (`Token`). Admin steps need `TIMEFORCODE_LIVE_TEST_ADMIN_SECRET`; `start-local.ps1` makes a random secret per run unless `TEST_ADMIN_ACCESS_SECRET` is exported first.
- **Why:** a camelCase assumption failed only against the real stack, and the running stack's secret cannot be recovered afterwards.
- **How to apply:** export `TEST_ADMIN_ACCESS_SECRET` before `podman compose up`, tag persistence scenarios `@live` plus `@liveonly` (skipped in-process), and give each scenario unique data.

## Authorization module

### Reqnroll steps share errors through `SessionContext`, and logout-style calls expire the client cookies

- **Date / issue:** 2026-10-17 / #107
- **Lesson:** `_exception` fields are private per step class, so a reused Then step ("The user is informed their profile was not found") sees nothing from a new When step; use `SessionContext.LastException`. An endpoint that expires the token cookies also removes them from the spec `CookieContainer`, so a "kept token" scenario must re-add the cookies after the call.
- **Why:** both cost a failed spec run.
- **How to apply:** set `LastException` in new When steps; copy `GetAllCookies()` before the call and re-add them to simulate a token the user kept.

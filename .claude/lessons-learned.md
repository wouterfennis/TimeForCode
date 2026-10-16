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

### Appending to a CRLF file with a shell rewrites the whole file

- **Date / issue:** 2026-10-13 / PR #98
- **Lesson:** Some tracked files (for example `Repositories.feature`) are committed with CRLF. Appending LF lines with `cat >>` or `sed -i` makes the working copy mixed, and `git add` then normalises the entire file to LF, so a 14-line addition shows as a 70-line diff.
- **Why:** `core.autocrlf=true` here, and the review flagged the noise.
- **How to apply:** check `git show HEAD:<file> | file -` before editing a file by shell; use the Edit tool (it keeps endings), or convert new lines to CRLF and `git -c core.autocrlf=false add <file>`. Confirm with `git diff --numstat`.
- **Exception (2026-10-15 / #110):** `.gitignore` is committed with mixed endings, so the Edit tool turned its trailing LF lines into CRLF. For mixed files, rebuild from `git show HEAD:<file>` and append with `printf`.

### Read guard and Node engines (front-end work)

- **Date / issue:** 2026-10-15 / #110
- **Lesson:** The read-guard hook blocks Bash reads (`grep`, `cat`, `ls`...) of `node_modules` and lockfiles, except a dependency's own `package.json`; append `# allow-excluded` when a wider read is intended. Local Node is 24.12, so packages needing 24.15 or later fail with `engine-strict`; pick a release that supports every Node 24.

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
- **Lesson:** `TimeForCode.Donation.Api.json` is regenerated on build and drives the generated client used by the Specifications and the Website; `SwaggerTests` then fails until the verified snapshot is replaced after review (skill `swagger-snapshot-review`).
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

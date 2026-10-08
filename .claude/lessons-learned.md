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
- **Lesson:** A lifecycle change that alters existing behaviour (register status, a removed endpoint) deserves one AskUserQuestion before coding.
- **Why:** the answer "replace the DELETE endpoint" collided with the issue's "no changes under src/Website" criterion, because `Profile.razor` called it; the user then accepted a broken Website.
- **How to apply:** grep `src/Website` and other consumers for an endpoint before removing it, and surface the conflict in the question.

## Tooling and environment

### Shell quirks on this machine

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** No Python is installed; large heredocs with apostrophes inside a single Bash call can fail to parse and run nothing; `sleep` followed by a command is blocked.
- **Why:** a failed parse silently skips the whole command, which looks like a no-op.
- **How to apply:** create files with the Write tool, keep Bash commands short, wait with `run_in_background` or Monitor.

### `dotnet format ./TimeForCode.sln` churns unrelated files

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** The solution-wide format (and builds that regenerate `*.Api.json`) modify files in other modules (Authorization, Shared, Website, tst/Website).
- **Why:** unrelated diffs pollute the PR and can break the "no changes under src/Website" criterion.
- **How to apply:** after formatting, run `git status --short` and `git checkout --` every path outside the issue's scope.

### Website specifications need the running stack

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** `TimeForCode.Website.Specifications` fails with `ERR_CONNECTION_REFUSED` on localhost:8083 unless the local services run (`scripts/start-local.ps1`).
- **Why:** it is an environment failure, not a regression; the solution-wide test run always shows it.
- **How to apply:** list it under Loose Ends; judge the Donation and Authorization test projects separately.

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
- **How to apply:** build the whole solution after any controller change; accept the snapshot only after reading the diff.

### Spec step classes can be split with `partial`

- **Date / issue:** 2026-10-08 / #75
- **Lesson:** Reqnroll step classes keep state in private fields (for example `_exception`), so shared Then-steps only see state in the same class. Split large step classes as `partial class` files (`ProjectSteps.Lifecycle.cs`).
- **Why:** a second step class would not see the first one's captured API error.
- **How to apply:** extend the existing partial class rather than adding a new binding class for the same feature area.

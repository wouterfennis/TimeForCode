---
paths:
  - "tst/**"
---

# Testing Strategy (TDD)

All production code in **TimeForCode** is written test-first. No production line exists without a failing test that demanded it.

## The TDD loop (mandatory)

1. **Red** — write the smallest test (unit, or step definitions for a Reqnroll scenario) that describes the next behaviour. Run it and confirm it fails **for the expected reason** (assertion or missing member, not a typo or setup error). A test that has never been seen failing proves nothing.
2. **Green** — write the minimum production code that makes it pass. No speculative code, no extra branches.
3. **Refactor** — clean up production and test code with the suite green. Run the project's tests after every refactor step.
4. Repeat. One behaviour per cycle; commit-sized steps.

Rules:

- Never write production code without a red test first. Exceptions (pure wiring such as DI registration, DTO mapping boilerplate, Bicep/Docker) must be exercised by an existing higher-level test (Specification or Infrastructure test) that is red first.
- Never edit a test to make it green. Change a test only when the requirement changed, and say so in the implementation log.
- Never commit a red test. A deliberately failing test is only allowed locally mid-cycle.
- Bug fixes start with a failing regression test that reproduces the bug (skill `fix-bug`).
- Outside-in order for a feature with a Gherkin file: Specification steps (red) → Application handler unit tests (red→green) → Domain/Infrastructure → API; the Specification goes green last.

## Test projects

Per module under `tst/<Module>/TimeForCode.<Module>.<Kind>/`:

| Kind | Purpose | Notes |
| --- | --- | --- |
| `Api.Tests`, `Application.Tests` | Unit tests of one class, all dependencies mocked | No I/O. `Api.Tests` also holds the Swagger Verify snapshot (`SwaggerTests`) |
| `Infrastructure.Tests` | Repository / external-service behaviour | Never use production credentials |
| `Specifications` | Reqnroll scenarios from the issue's Gherkin comment, run against `TimeForCodeWebApplicationFactory` | Outside-in acceptance tests |
| `Architecture.Tests` | ArchUnitNET layer and naming rules | Violations break the build |

Frameworks: **MSTest**, AwesomeAssertions, Moq, Verify.MSTest, Reqnroll, ArchUnitNET. Do not introduce xUnit/NUnit/NetArchTest.
Not every module has every kind (for example `Shared.Tests`); check `repo-map` instead of assuming.

## Conventions

- Test class: `<ClassUnderTest>Tests`. Test method: `<MethodUnderTest>_<Condition>_<ExpectedBehaviour>`, e.g. `Handle_ValidCommand_ReturnsSuccess`.
- Arrange / Act / Assert, one logical assertion per test, no logic (loops, conditionals) in tests.
- Reqnroll step text uses the personas "The user", "The external platform", "The time for code platform"; no HTTP verbs, status codes, class names or signatures. Step classes are `<Feature>Steps`. Reuse existing `[Binding]` steps before writing new ones (grep first).
- Every new handler and validator gets a positive and a negative-path unit test; every Gherkin scenario gets bound steps.
- Test data inline or in `TestData/`; deterministic (fixed GUIDs); no shared mutable state between tests.
- `[Ignore]` is allowed only with `// TODO(review): flaky - <reason>` and an entry under Loose Ends.

## Step style (Reqnroll)

Run `pwsh scripts/step-catalog.ps1 -Steps` before adding a step: it lists every step with its usage count and definition file, and reports unused definitions. Baseline numbers: `docs/current/reqnroll-baseline.md`.

- **One rejection vocabulary.** Express a rejected request with the existing generic rejection steps, not one step per endpoint; add a status-only rejection step only when no generic one fits.
- **Parameterize, do not copy.** A step that differs from an existing one in a value or a word becomes one step with a `{string}`/`{int}`/`{word}` parameter (or an alternation in a regex), not a second step.
- **Tables for optional fields.** Three or more optional fields in a Given or When go in a data table, not in a chain of `And` steps or a step per combination.
- **`Background` for shared preambles.** Steps repeated at the start of every scenario in a feature move to `Background`.
- **Delete what the catalog reports unused**, and keep the step count per feature no higher than the scenarios need.

## Specs: shared state and fakes

- **Assertions:** `using AwesomeAssertions;`, never `FluentAssertions` (8+ is commercially licensed). After a bulk rename grep the tree for the old name; some files start with a BOM, so anchored `sed` misses line 1. Compare `BsonDocument` through `ToJson()`, since the library treats it as a collection of elements.
- **Donation:** one `[Binding]` class per aggregate. State several classes need (`LastException`, list results, ids) lives in `DonationContext`, shared arrange helpers in `DonationFixtures`; both are injected. Put a failed response through `_context.CaptureAsync`, never into a private field.
- **Authorization:** a failed call goes into `SessionContext.LastException`, so reused Then steps see it. An endpoint that expires the token cookies also removes them from the spec `CookieContainer`: copy `GetAllCookies()` before the call and re-add them to simulate a kept token.
- **Catalog fakes:** give each project in an in-memory catalog its own id (`ProjectBuilder.IdFor(n)`); `BuildActive()` and the lifecycle project share `Constants.TestProjectId`. Render Mongo filters with `RenderArgs<T>` (driver 3.x) and compare JSON.
- **Admin steps:** the step "The user has an administrator access token" builds a `scope=admin` token.
- **`@live` scenarios** sign in through the real login (Auth login, mock IdP confirm form, callback) and read the `AccessToken` cookie, whose JSON is PascalCase (`Token`). Admin steps need `TIMEFORCODE_LIVE_TEST_ADMIN_SECRET`: export `TEST_ADMIN_ACCESS_SECRET` before `podman compose up` (`start-local.ps1` otherwise makes a random one that cannot be recovered). Tag persistence scenarios `@live` plus `@liveonly` and give each scenario unique data.

## Running tests

Follow skill `quiet-dotnet`: scope to one project while iterating (`dotnet test tst/<Module>/<Project> --filter "FullyQualifiedName~<Name>"`), full solution once at the end. The trimming hook prints only failures. Delegate big runs to the `test-runner` subagent.

Swagger snapshot diffs are never accepted automatically: skill `swagger-snapshot-review`.

## Quality gates

| Gate | Command | Pass |
| --- | --- | --- |
| Red seen | Run the new test before the production change | Fails for the expected reason |
| Green | `dotnet test tst/<Module>/<Project>` | 0 failures |
| Final | `dotnet build TimeForCode.sln` then `dotnet test TimeForCode.sln --no-build` | Exit 0 (once, at the end or in review) |
| No silent skips | Grep `\[Ignore\]` in `tst/` | None without `TODO(review)` |

A failing test that pre-dates the branch is listed under Loose Ends with a link; it is not "fixed" by weakening the test.

# Write-feature reference

Detail for `/write-feature`. Read the section you need.

## Specification project layout

Each module with Reqnroll specs has `tst/<Module>/TimeForCode.<Module>.Specifications/` (currently Authorization, Donation, Website; verify with `repo-map`):

| Folder | Contents |
| --- | --- |
| `Features/<Capability>/` | `.feature` files, grouped by capability (PascalCase folder) |
| `Steps/` | Step classes `<Feature>Steps.cs` |
| `Mocking/` | Web application factory and test infrastructure |
| `TestBuilder/` | Test data builders |

Intended path of a new file: `tst/<Module>/TimeForCode.<Module>.Specifications/Features/<Folder>/<FeatureName>.feature`. Pick the module from the issue's affected area; reuse an existing folder or propose a new one.

## Step 2 — Exploration

Glob the `Features/` tree and Grep `*Steps.cs` for candidate step text. Read only the matching feature and step files. Record steps reusable verbatim, steps that need new definitions (note, never write code), and the target folder. Check `docs/current/` for API/capability context.

## Gherkin structure

```gherkin
Feature: <Short feature name>
    As a <persona>
    I want to <goal>
    So that <benefit>

Scenario: <Descriptive, human-readable title>
    Given <precondition>
    When <action>
    Then <outcome>
    And <additional outcome>
```

Use `Scenario Outline` with `Examples:` only when scenarios differ solely in data.

DO: plain English from the actor's view; reuse exact wording of existing steps; one observable action or state per step; business value in the `Feature:` block; descriptive scenario titles; `And`/`But` instead of repeating keywords; quoted values for variable text.

DO NOT: reference class names, method names, HTTP verbs/status codes, tables or internal identifiers; write steps that need implementation knowledge; use vague steps ("the system is set up"); duplicate existing scenarios; use more than one `When` per scenario; use passive voice when active works.

## Project conventions

Personas: **The user**, **The external platform**, **The time for code platform**.

```gherkin
# Correct
Given The user has an account at the external platform
When The user logs in at the time for code platform
Then The user is redirected to the external platform

# Wrong
Given a user account exists in the system
When POST /auth/login is called
Then response status is 302
```

Parameterised text uses `{string}` in definitions: `Then The following callback error message is returned: "State is not known"`. Indent steps with a single tab, like the existing files.

## Step 5 — Self-verification

| # | Check | Pass condition |
| --- | --- | --- |
| 1 | Feature header | Has As a / I want / So that with business value |
| 2 | Scenario titles | Complete plain-English sentences |
| 3 | Step reuse | Existing wording used wherever the meaning matches |
| 4 | No implementation language | No classes, HTTP, DB terms, identifiers |
| 5 | One When | Exactly one `When` per scenario |
| 6 | Testability | Each scenario has a specific, independently verifiable `Then` |
| 7 | Intended path | Recorded and included in the comment |
| 8 | New steps noted | Listed for the developer |
| 9 | AC coverage | Every acceptance criterion traces to a scenario |

## Steps 6-7 — Present and post

Show the feature file, the intended path, the new-step list and the reused-step list. AskUserQuestion: does it capture the issue's scenarios, and may it be posted to issue #N. Loop on changes (rerun the checklist). Post only after explicit confirmation, with skill `gh-issue` (mode `comment-feature`), then report the URL.

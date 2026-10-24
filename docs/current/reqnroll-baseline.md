# Reqnroll baseline

Status: Current

Measured baseline of the Reqnroll specification projects, the reference point for the step-reduction slices of epic #155. Every slice reports its change against these numbers.

## Method

The counts come from `pwsh scripts/step-catalog.ps1 -Markdown`. It reads the step attributes in `tst/<Module>/TimeForCode.<Module>.Specifications` and the `.feature` files, and counts:

- **Scenarios**: every `Scenario` plus every row of an `Examples` table.
- **Step lines**: step lines in the feature files as written (`Background` steps once).
- **Distinct step texts**: unique step texts as written.
- **Step attributes**: `Given`, `When`, `Then` and `StepDefinition` attributes in C#.
- **Hand-written C# lines**: non-blank lines in the project's `.cs` files, excluding `bin`, `obj` and generated `*.feature.cs`.
- **Feature lines**: all lines in the `.feature` files.

`pwsh scripts/step-catalog.ps1 -Steps` adds every step definition with its file and usage count.

## Baseline

Measured on 2026-10-24.

| Module | Scenarios | Step lines | Distinct step texts | Step attributes | Hand-written C# lines | Feature lines |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Authorization | 75 | 312 | 134 | 133 | 2016 | 541 |
| Donation | 182 | 573 | 260 | 231 | 3417 | 1073 |
| Total | 257 | 885 | 394 | 364 | 5433 | 1614 |

## Passing scenarios

Run with `dotnet test <spec project> --filter "FullyQualifiedName~.Features."`, which selects the scenarios and leaves out the hand-written host tests.

| Module | Scenarios | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| Authorization | 75 | 75 | 0 | 0 |
| Donation | 182 | 172 | 0 | 10 |
| Total | 257 | 247 | 0 | 10 |

The 10 skipped Donation scenarios are skipped in a plain run; they are the `@live` scenarios that need the real stack.

## Unused step definitions

- Authorization: `Steps/RefreshSteps.cs:88`, "The access token is revoked".
- Donation: none.

# Implementation reference

Per-layer details for `/implement-issue`. Read only the section you need.

## Layer order and what each step adds

Outside-in; every step starts with a red test.

| Step | Where | Rules |
| --- | --- | --- |
| Specification steps | `tst/<Module>/TimeForCode.<Module>.Specifications/Steps/` | Grep existing `[Binding]` steps and reuse them verbatim before writing new ones. New external HTTP: `MockHttpMessageHandler` set up in the step via `_provider.GetRequiredService<MockHttpMessageHandler>()`. New repository mock: register in `TimeForCodeWebApplicationFactory.ConfigureWebHost` (remove real, add `Mock<INewRepository>`) |
| Domain | `...Domain/Entities/` | Prefer new entities/properties over changing existing ones. No outward dependencies |
| Commands | `...Commands/` | `IRequest<Result<T>>`; result types are plain records |
| Application interfaces | `...Application/Interfaces/` | Interface only; implementation belongs to Infrastructure |
| Application handlers | `...Application/Handlers/` | `internal`, `IRequestHandler<TCommand, Result<T>>`, return `Result<T>.Success/Failure("message")`, never catch generic exceptions |
| Infrastructure | `...Infrastructure/` | Register in `AddInfrastructureLayer` (`ServiceCollectionExtensions.cs`), never from a controller or handler. New collection/index: run skill `mongo-review` |
| API | `...Api/` | Correct `[HttpX]` and `[ProducesResponseType]`, request→command mapper, `Result<T>` failure → `BadRequest(ProblemDetails)`, success → 2xx |
| Architecture tests | `...Architecture.Tests` | Add ArchUnitNET rules only when a new structural pattern is introduced |

## No feature file

Start with a handler unit test (`[TestClass]` in `Api.Tests`/`Application.Tests`/`Infrastructure.Tests`), see it red, then implement.

## Marking uncertainty

```csharp
// TODO(review): <what is unknown and what assumption was made>
var placeholder = default!; // replace with actual value
```

Use exactly `// TODO(review):` so it is greppable. Each one is logged as a Loose End.

## Swagger snapshot failures

A `VerifyException` from `SwaggerTests` is an API contract gate. Never accept the snapshot automatically; follow skill `swagger-snapshot-review` and record the checklist in the log.

## Docs to touch before the log

Update in the same branch when applicable: `docs/current/api-surface.md` (endpoint changed), `docs/current/capability-status.md` (capability implemented), arc42 chapters 05/06/08/09/11 (see `.claude/rules/docs.md`).

## Implementation log content

- **Completed** — one bullet per finished item with a relative file path.
- **TDD evidence** — which tests were seen red first, then green.
- **Loose Ends** — each `TODO(review)` (file + line), each intentionally pending item, anything not reached.
- **Open Questions** — deferred domain or design decisions.

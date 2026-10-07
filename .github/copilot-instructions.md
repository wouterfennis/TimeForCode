# TimeForCode — repo map

.NET 10 solution `TimeForCode.sln`; bounded contexts under `src/` and `tst/`: **Authorization**, **Donation**, **Shared**, **Website**. Deploy IaC in `deploy/`, docs in `docs/` (arc42 in `docs/architecture/arc42/`, live state in `docs/current/`).

| Layer (per module) | Path |
|--------------------|------|
| API / Commands / Application / Domain / Infrastructure / Values | `src/<Module>/TimeForCode.<Module>.<Layer>/` |
| Specs (Reqnroll) / Unit (`Api.Tests`, `Application.Tests`) / Infra / Architecture tests | `tst/<Module>/TimeForCode.<Module>.<Kind>/` |

Stack: MediatR + `Result<T>`, MongoDB, MSTest + FluentAssertions + Moq, Reqnroll, ArchUnitNET layer rules (violations break the build). Style comes from `.editorconfig` — run `dotnet format ./TimeForCode.sln` before committing C#.

Commands: `dotnet build TimeForCode.sln` · `dotnet test tst/<Module>/<Project>` (target one project, not the whole solution, until the final check) · `gh issue view <n> --json ...` (see `gh-compact-view` skill).

Detail on demand (load only when needed): skills `repo-map`, `dotnet-conventions`, `quiet-dotnet`, `gh-compact-view`, `context-handoff`; token rules in `.github/instructions/token-efficiency.instructions.md`.

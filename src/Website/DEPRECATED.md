# Website is deprecated

The Blazor Website (`src/Website/` and `tst/Website/`) is deprecated as of 2026-10-13.

- It is no longer part of `TimeForCode.sln`, so `dotnet build`, `dotnet test` and `dotnet format` on the solution skip it.
- The CI workflows (`build.yaml`, `pullRequest.yaml`) no longer build or push its Docker image.
- It does not compile against the current Donation API client (`GetProjectsAsync` gained search parameters in #76) and is not maintained.
- The code stays in the repository for reference; `Dockerfile.website` and the local compose files are unchanged.

New front-end work happens against the documented API contracts in `docs/current/api-surface.md`.

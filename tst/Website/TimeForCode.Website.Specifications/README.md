# TimeForCode.Website.Specifications

Browser-level acceptance tests for the TimeForCode website, written with [Reqnroll](https://reqnroll.net/) (Gherkin / BDD) and [Microsoft Playwright](https://playwright.dev/dotnet/) for browser automation.

## Scope

This project tests user-facing journeys through the browser. It does **not** reference any Blazor or Website source assemblies. All selectors use `data-testid` attributes or ARIA roles so they remain valid after the frontend migrates away from Blazor.

| Feature | Description |
| --- | --- |
| `Home` | Unauthenticated visitor sees the login link and the published-projects section |
| `Projects` | Visitor browses `/projects` and sees tiles that link to the detail page |
| `ProjectDetail` | Visitor navigates to a project detail URL and sees the heading and back link |
| `Login` | OAuth login via the Identity Provider Mock lands the user back on the home page |
| `Profile` | Logged-in user sees their GitHub login handle on `/profile` |
| `Logout` | Logged-in user logs out and returns to the unauthenticated home page |

## Prerequisites

The full Docker Compose stack must be running before executing these tests:

```bash
podman compose up --build
```

The tests expect:

| Service | Default URL | Override via |
| --- | --- | --- |
| Website | `http://localhost:8083` | `WEBSITE_BASE_URL` env var |
| Identity Provider Mock | `http://localhost:8081` | `IDP_MOCK_BASE_URL` env var |

## Running the tests

```bash
# From the repository root
dotnet test tst/Website/TimeForCode.Website.Specifications/

# Or from this directory
dotnet test
```

To target a different environment, set the environment variables before running:

```bash
WEBSITE_BASE_URL=http://myhost:8083 dotnet test
```

### Playwright browser installation

On a fresh machine, install the required Playwright browser binaries once:

```bash
pwsh tst/Website/TimeForCode.Website.Specifications/bin/Debug/net10.0/playwright.ps1 install chromium
```

### Trace Viewer

Every scenario automatically records a Playwright trace. After a test run, one `.zip` file per scenario is written to:

```text
tst/Website/TimeForCode.Website.Specifications/bin/Debug/net10.0/TestResults/traces/
```

Open a trace in the interactive Trace Viewer (screenshots, DOM snapshots, network calls, timeline):

```powershell
pwsh tst/Website/TimeForCode.Website.Specifications/bin/Debug/net10.0/playwright.ps1 show-trace `
  "tst/Website/TimeForCode.Website.Specifications/bin/Debug/net10.0/TestResults/traces/<scenario-title>.zip"
```

## Relationship to other specification projects

| Project | Scope | Mechanism |
| --- | --- | --- |
| `TimeForCode.Authorization.Specifications` | OAuth token issuance, session management | HTTP via `WebApplicationFactory` |
| `TimeForCode.Donation.Specifications` | Project publishing, listing, unpublishing | HTTP via `WebApplicationFactory` |
| `TimeForCode.Website.Specifications` *(this project)* | User-facing browser journeys | Browser automation via Playwright |

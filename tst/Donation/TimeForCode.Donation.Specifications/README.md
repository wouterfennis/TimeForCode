# TimeForCode.Donation.Specifications

BDD-style acceptance tests for the Donation API, written with [Reqnroll](https://reqnroll.net/) (Gherkin) and MSTest. Tests run entirely in-process — no external services or databases are required.

## Scope

| Feature | Description |
| --- | --- |
| `PublishProject` | Authenticated user publishes a GitHub repository as a project on the platform |
| `BrowseProjects` | User browses the paginated list of published projects |
| `UnpublishProject` | Authenticated user unpublishes (archives) a project they own |

## How the test host works

### WebApplicationFactory

`Mocking/TimeForCodeWebApplicationFactory.cs` extends `WebApplicationFactory<Startup>` to spin up the Donation API in-process. Inside `ConfigureWebHost` it:

1. **Removes real infrastructure registrations** — `IMongoDbContext`, `IProjectRepository`, `IGithubRepositoryApiService`, and the `RestClient` are stripped from the DI container so no live database or GitHub API is ever contacted.
2. **Registers Moq mocks** — `Mock<IProjectRepository>` and `Mock<IGithubRepositoryApiService>` are added as singletons so individual step definitions can set up and verify interactions per-scenario.
3. **Wires MockHttp** — a `MockHttpMessageHandler` (from [`RichardSzalay.MockHttp`](https://github.com/richardszalay/mockhttp)) replaces the underlying HTTP handler of the `RestClient`. Individual steps configure expectations on this handler to control responses from the simulated GitHub API.
4. **Overrides JWT validation** — the `JwtBearerOptions` are reconfigured to accept symmetric HMAC tokens signed with a fixed test key (`Constants.TestJwtSigningKey`). This allows step definitions to issue test tokens without a running Authorization API or RSA key pair.

### BearerTokenHandler

`Mocking/BearerTokenHandler.cs` is a `DelegatingHandler` that injects a `Bearer` token into every outgoing HTTP request made by the generated `IDonationClient`. Steps in `AuthenticationSteps.cs` configure whether a valid token or no token is attached, making authentication scenarios straightforward.

## Mocking strategy

| Dependency | How it is mocked |
| --- | --- |
| MongoDB / `IProjectRepository` | `Moq` — setup per-scenario in step definitions |
| GitHub REST API / `IGithubRepositoryApiService` | `Moq` — setup per-scenario in step definitions |
| Authorization / JWT validation | Symmetric test key via `PostConfigure<JwtBearerOptions>` |
| Outgoing HTTP (RestClient) | `MockHttpMessageHandler` from `RichardSzalay.MockHttp` |

## Running the tests

```bash
# From the repository root
dotnet test tst/Donation/TimeForCode.Donation.Specifications/

# Or from this directory
dotnet test
```

No stack needs to be running. All dependencies are mocked in-process.

## Relationship to other specification projects

| Project | Scope | Mechanism |
| --- | --- | --- |
| `TimeForCode.Authorization.Specifications` | OAuth token issuance, session management | HTTP via `WebApplicationFactory` |
| `TimeForCode.Donation.Specifications` *(this project)* | Project publishing, listing, unpublishing | HTTP via `WebApplicationFactory` |
| `TimeForCode.Website.Specifications` | User-facing browser journeys | Browser automation via Playwright |

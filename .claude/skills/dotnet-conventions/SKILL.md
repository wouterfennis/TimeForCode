---
name: dotnet-conventions
description: "TimeForCode .NET stack facts, layer rules, project paths, Reqnroll/MSTest conventions, naming and code templates. Use when: writing handlers, controllers, repositories, step definitions or unit tests."
---

# dotnet-conventions

Reference for writing TimeForCode C# and tests. Moved out of the Implementation agent so it only loads when needed.

## Technology Stack Reference

Use these facts during every implementation session. Do not make assumptions that contradict them.

### Runtime & Frameworks

- **Target framework:** `net10.0`, nullable reference types enabled, implicit usings enabled
- **Dependency injection:** Microsoft.Extensions.DependencyInjection (constructor injection everywhere)
- **Mediator:** MediatR — all application logic is triggered via `IMediator.Send(command)`
- **Persistence:** MongoDB via `MongoDB.Driver` — entities inherit `DocumentEntity` (which carries an `ObjectId Id`)
- **HTTP clients to external services:** RestSharp `RestClient` (not `HttpClient`)
- **Authentication:** JWT Bearer with RSA key validation; policy `"ApiUser"` requires `scope: user` claim
- **Error responses:** Always `ProblemDetails` — never raw strings or custom error models

### Result Pattern

Application handlers return `Result<T>`:

```csharp
Result<T>.Success(value)
Result<T>.Failure("error message")
```

API controllers map `Result<T>` to HTTP responses. Failures map to `BadRequest(ProblemDetails)`.

### Layer Rules (enforced by ArchUnitNET tests — violations break the build)

| Layer | Project | May depend on |
| --- | --- | --- |
| API | `TimeForCode.*.Api` | Application, Domain, Values |
| Application | `TimeForCode.*.Application` | Domain, Commands, Values |
| Domain | `TimeForCode.*.Domain` | (nothing in this solution) |
| Infrastructure | `TimeForCode.*.Infrastructure` | Application (interfaces), Domain, Values |
| Commands | `TimeForCode.*.Commands` | (nothing in this solution) |
| Values | `TimeForCode.*.Values` | (nothing in this solution) |

### Project File Locations

| Purpose | Path |
| --- | --- |
| API controllers & models | `src/<Module>/TimeForCode.<Module>.Api/` |
| MediatR commands & results | `src/<Module>/TimeForCode.<Module>.Commands/` |
| Handlers, interfaces, services | `src/<Module>/TimeForCode.<Module>.Application/` |
| Domain entities | `src/<Module>/TimeForCode.<Module>.Domain/Entities/` |
| Infrastructure (repositories, external services) | `src/<Module>/TimeForCode.<Module>.Infrastructure/` |
| Value objects / enums | `src/<Module>/TimeForCode.<Module>.Values/` |
| Reqnroll specifications | `tst/<Module>/TimeForCode.<Module>.Specifications/` |
| Unit tests | `tst/<Module>/TimeForCode.<Module>.Api.Tests/` |
| Infrastructure tests | `tst/<Module>/TimeForCode.<Module>.Infrastructure.Tests/` |
| Architecture tests | `tst/<Module>/TimeForCode.<Module>.Architecture.Tests/` |

### Testing Stack

- **Test framework:** MSTest (`[TestClass]`, `[TestMethod]`, `[TestInitialize]`)
- **Assertions:** FluentAssertions — always use `.Should()` chains
- **Mocking:** Moq — `Mock<T>`, `.Setup(...)`, `.Verify(...)`
- **BDD runner:** Reqnroll — step classes carry `[Binding]`, dependencies are constructor-injected via BoDi
- **Integration host:** `WebApplicationFactory<Startup>` in `Mocking/TimeForCodeWebApplicationFactory.cs`
- **Snapshot testing:** Verify library (used in Swagger tests)

### Reqnroll-Specific Conventions

- Each scenario gets a fresh `BeforeScenario` hook — state is per-scenario, not per-feature
- Use the existing `IAuthClient` (NSwag-generated) to call the API under test from step definitions
- New mock setups for external HTTP calls go in the step definition file using the `MockHttpMessageHandler` resolved from `IServiceProvider`
- New repository mocks are registered inside `TimeForCodeWebApplicationFactory.ConfigureWebHost`
- Step text must follow the established persona conventions (see below)

### Step Text Personas

| Actor | Step subject |
| --- | --- |
| End user | `The user` |
| External OAuth provider | `The external platform` |
| This system | `The time for code platform` |

## Coding Conventions

Follow these conventions exactly. They are not negotiable.

### File organisation

- One class per file; file name matches class name
- Namespace matches folder path (e.g., `TimeForCode.Authorization.Application.Handlers`)
- `using` directives sorted alphabetically; no unused usings

### Naming

- Commands: `<Verb><Noun>Command` (e.g., `CreateDonationCommand`)
- Handlers: `<Verb><Noun>Handler` (e.g., `CreateDonationHandler`)
- Interfaces: `I<Noun>` (e.g., `IDonationRepository`)
- Step classes: `<Feature>Steps` (e.g., `DonationSteps`)
- Test classes: `<ClassUnderTest>Tests` (e.g., `LoginHandlerTests`)

### Test method naming

```text
<MethodUnderTest>_<Condition>_<ExpectedBehaviour>
// example:
HandleAsync_WithExpiredToken_ReturnsFailure
```

### MSTest structure

```csharp
[TestClass]
public class MyHandlerTests
{
    private Mock<IMyRepository> _repositoryMock = default!;
    private MyHandler _sut = default!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IMyRepository>();
        _sut = new MyHandler(_repositoryMock.Object);
    }

    [TestMethod]
    public async Task HandleAsync_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        ...

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

### Reqnroll step class structure

```csharp
[Binding]
internal class NewFeatureSteps
{
    private readonly IAuthClient _authClient;
    private readonly IServiceProvider _provider;

    public NewFeatureSteps(IAuthClient authClient, IServiceProvider provider)
    {
        _authClient = authClient;
        _provider = provider;
    }

    [Given("step text matching feature file exactly")]
    public async Task GivenStepTextAsync()
    {
        ...
    }
}
```

### Error responses

```csharp
return BadRequest(new ProblemDetails
{
    Title = "Short title",
    Detail = "Human-readable explanation",
    Status = StatusCodes.Status400BadRequest
});
```

### Infrastructure registration

All new services must be added to the existing `AddInfrastructureLayer` extension method in `TimeForCode.<Module>.Infrastructure/ServiceCollectionExtensions.cs`. Never call `services.Add*` from a controller or handler.

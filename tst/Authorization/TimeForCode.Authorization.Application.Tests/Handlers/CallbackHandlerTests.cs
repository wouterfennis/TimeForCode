using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Tests.Handlers
{
    [TestClass]
    public class CallbackHandlerTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        private Mock<IStateRepository> _stateRepositoryMock = null!;
        private Mock<ILoginCodeRepository> _loginCodeRepositoryMock = null!;
        private Mock<IAccountService> _accountServiceMock = null!;
        private Mock<ITokenService> _tokenServiceMock = null!;
        private CallbackHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _stateRepositoryMock = new Mock<IStateRepository>();
            _loginCodeRepositoryMock = new Mock<ILoginCodeRepository>();
            _accountServiceMock = new Mock<IAccountService>();
            _tokenServiceMock = new Mock<ITokenService>();
            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);
            var randomGenerator = new Mock<IRandomGenerator>();
            randomGenerator.Setup(x => x.GenerateRandomString()).Returns("generated-code");
            var options = Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                ValidRedirectUris = [],
                LoginCodeLifetimeInSeconds = 45
            });

            _sut = new CallbackHandler(
                _stateRepositoryMock.Object,
                _loginCodeRepositoryMock.Object,
                randomGenerator.Object,
                options,
                timeProvider.Object,
                _accountServiceMock.Object,
                _tokenServiceMock.Object,
                NullLogger<CallbackHandler>.Instance);
        }

        private static StateEntry State(DateTimeOffset expiresAfter) => new()
        {
            Id = ObjectId.GenerateNewId(),
            Key = "state",
            IdentityProvider = IdentityProvider.Github,
            RedirectUri = new Uri("https://localhost/"),
            CodeVerifier = "verifier",
            ClientCodeChallenge = "client-challenge",
            ExpiresAfter = expiresAfter
        };

        private static AccountInformation Account() => new()
        {
            Id = ObjectId.GenerateNewId(),
            IdentityProviderId = "42",
            Login = "octocat",
            NodeId = "node",
            AvatarUrl = "https://avatars.githubusercontent.com/u/42",
            Name = "Octo Cat",
            Company = null,
            Email = "octo@example.com",
            Bio = null,
            Location = null
        };

        private AccountInformation ArrangeSuccessfulLogin(StateEntry entry, bool isNewAccount)
        {
            var account = Account();
            _stateRepositoryMock.Setup(x => x.TakeAsync("state")).ReturnsAsync(entry);
            _tokenServiceMock.Setup(x => x.GetAccessTokenFromExternalProviderAsync(entry, "code"))
                .ReturnsAsync(Result<ExternalAccessToken>.Success(new ExternalAccessToken { Token = "external" }));
            _accountServiceMock.Setup(x => x.SaveAccountInformation(IdentityProvider.Github, It.IsAny<ExternalAccessToken>()))
                .ReturnsAsync(Result<SaveAccountResult>.Success(new SaveAccountResult(account, IsNewAccount: isNewAccount)));
            return account;
        }

        [TestMethod]
        public async Task Handle_SuccessfulLogin_ReturnsTheOneTimeCodeAndTheStoredRedirectUri()
        {
            var entry = State(Now.AddMinutes(5));
            ArrangeSuccessfulLogin(entry, isNewAccount: false);

            var result = await _sut.Handle(new CallbackCommand { Code = "code", State = "state" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Code.Should().Be("generated-code");
            result.Value.RedirectUri.Should().Be(entry.RedirectUri);
        }

        [TestMethod]
        public async Task Handle_SuccessfulLogin_StoresOnlyTheHashOfTheCodeBoundToTheClientChallengeAndTheUser()
        {
            var entry = State(Now.AddMinutes(5));
            var account = ArrangeSuccessfulLogin(entry, isNewAccount: true);
            LoginCode? stored = null;
            _loginCodeRepositoryMock.Setup(x => x.AddAsync(It.IsAny<LoginCode>()))
                .Callback<LoginCode>(c => stored = c)
                .Returns(Task.CompletedTask);

            await _sut.Handle(new CallbackCommand { Code = "code", State = "state" }, CancellationToken.None);

            stored.Should().NotBeNull();
            stored!.CodeHash.Should().Be(RefreshTokenHasher.Hash("generated-code")).And.NotBe("generated-code");
            stored.CodeChallenge.Should().Be("client-challenge");
            stored.UserId.Should().Be(account.Id.ToString());
            stored.IsNewUser.Should().BeTrue();
            stored.ExpiresAfter.Should().Be(Now.AddSeconds(45));
        }

        [TestMethod]
        public async Task Handle_SuccessfulLogin_IssuesNoTokens()
        {
            ArrangeSuccessfulLogin(State(Now.AddMinutes(5)), isNewAccount: false);

            await _sut.Handle(new CallbackCommand { Code = "code", State = "state" }, CancellationToken.None);

            _tokenServiceMock.Verify(x => x.GenerateInternalToken(It.IsAny<AccountInformation>()), Times.Never);
            _tokenServiceMock.Verify(x => x.GenerateInternalTokenForSessionAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_UnknownState_FailsWithoutCallingTheExternalProvider()
        {
            _stateRepositoryMock.Setup(x => x.TakeAsync("state")).ReturnsAsync((StateEntry?)null);

            var result = await _sut.Handle(new CallbackCommand { Code = "code", State = "state" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _tokenServiceMock.Verify(x => x.GetAccessTokenFromExternalProviderAsync(It.IsAny<StateEntry>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_ExpiredState_FailsWithoutCallingTheExternalProvider()
        {
            _stateRepositoryMock.Setup(x => x.TakeAsync("state")).ReturnsAsync(State(Now.AddSeconds(-1)));

            var result = await _sut.Handle(new CallbackCommand { Code = "code", State = "state" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _tokenServiceMock.Verify(x => x.GetAccessTokenFromExternalProviderAsync(It.IsAny<StateEntry>(), It.IsAny<string>()), Times.Never);
        }
    }
}
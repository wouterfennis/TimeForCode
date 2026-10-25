using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Tests.Handlers
{
    [TestClass]
    public class CallbackHandlerTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        private Mock<IStateRepository> _stateRepositoryMock = null!;
        private Mock<IAccountService> _accountServiceMock = null!;
        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<IRefreshTokenService> _refreshTokenServiceMock = null!;
        private CallbackHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _stateRepositoryMock = new Mock<IStateRepository>();
            _accountServiceMock = new Mock<IAccountService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);

            _sut = new CallbackHandler(
                _stateRepositoryMock.Object,
                timeProvider.Object,
                _accountServiceMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                NullLogger<CallbackHandler>.Instance);
        }

        private static StateEntry State(DateTimeOffset expiresAfter) => new()
        {
            Id = ObjectId.GenerateNewId(),
            Key = "state",
            IdentityProvider = IdentityProvider.Github,
            RedirectUri = new Uri("https://localhost/"),
            CodeVerifier = "verifier",
            ExpiresAfter = expiresAfter
        };

        [TestMethod]
        public async Task Handle_SuccessfulLogin_IssuesTokenForTheSavedAccount()
        {
            var account = new AccountInformation
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
            var entry = State(Now.AddMinutes(5));
            _stateRepositoryMock.Setup(x => x.TakeAsync("state")).ReturnsAsync(entry);
            _tokenServiceMock.Setup(x => x.GetAccessTokenFromExternalProviderAsync(entry, "code"))
                .ReturnsAsync(Result<ExternalAccessToken>.Success(new ExternalAccessToken { Token = "external" }));
            _accountServiceMock.Setup(x => x.SaveAccountInformation(IdentityProvider.Github, It.IsAny<ExternalAccessToken>()))
                .ReturnsAsync(Result<SaveAccountResult>.Success(new SaveAccountResult(account, IsNewAccount: false)));
            _tokenServiceMock.Setup(x => x.GenerateInternalToken(account))
                .Returns(new AccessToken { Token = "jwt", ExpiresAfter = DateTimeOffset.UtcNow.AddHours(1) });
            _refreshTokenServiceMock.Setup(x => x.CreateRefreshTokenAsync(account.Id.ToString()))
                .ReturnsAsync(new Values.RefreshToken { Token = "refresh", ExpiresAfter = DateTimeOffset.UtcNow.AddDays(7) });

            var result = await _sut.Handle(new CallbackCommand { Code = "code", State = "state" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.InternalAccessToken.Token.Should().Be("jwt");
            result.Value.RedirectUri.Should().Be(entry.RedirectUri);
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
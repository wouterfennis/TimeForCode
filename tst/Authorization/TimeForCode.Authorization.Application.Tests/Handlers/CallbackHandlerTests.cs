using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Authorization.Application.Handlers;
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
        private Mock<IAccountService> _accountServiceMock = null!;
        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<IRefreshTokenService> _refreshTokenServiceMock = null!;
        private CallbackHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _accountServiceMock = new Mock<IAccountService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();

            _sut = new CallbackHandler(
                _accountServiceMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                NullLogger<CallbackHandler>.Instance);
        }

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
            _tokenServiceMock.Setup(x => x.GetAccessTokenFromExternalProviderAsync("state", "code"))
                .ReturnsAsync(Result<ExternalAccessToken>.Success(new ExternalAccessToken { Token = "external" }));
            _accountServiceMock.Setup(x => x.SaveAccountInformation("state", It.IsAny<ExternalAccessToken>()))
                .ReturnsAsync(Result<SaveAccountResult>.Success(new SaveAccountResult(account, IsNewAccount: false)));
            _tokenServiceMock.Setup(x => x.GenerateInternalToken(account))
                .Returns(new AccessToken { Token = "jwt", ExpiresAfter = DateTimeOffset.UtcNow.AddHours(1) });
            _refreshTokenServiceMock.Setup(x => x.CreateRefreshTokenAsync(account.Id.ToString()))
                .ReturnsAsync(new Values.RefreshToken { Token = "refresh", ExpiresAfter = DateTimeOffset.UtcNow.AddDays(7) });
            _tokenServiceMock.Setup(x => x.GetRedirectUri("state")).Returns(new Uri("https://localhost/"));

            var result = await _sut.Handle(new CallbackCommand { Code = "code", State = "state" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.InternalAccessToken.Token.Should().Be("jwt");
        }
    }
}
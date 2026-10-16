using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    [TestClass]
    public class TokenServiceSessionTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

        private Mock<IAccountInformationRepository> _accountRepositoryMock = null!;
        private RSA _rsa = null!;
        private TokenService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _rsa = RSA.Create(2048);
            _accountRepositoryMock = new Mock<IAccountInformationRepository>();

            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);

            _sut = new TokenService(
                _rsa,
                new Mock<IIdentityProviderServiceFactory>().Object,
                timeProvider.Object,
                new Mock<IStateRepository>().Object,
                _accountRepositoryMock.Object,
                Microsoft.Extensions.Options.Options.Create(new TokenCreationOptions
                {
                    Issuer = "issuer",
                    Audiences = ["audience"],
                    TokenExpiresInMinutes = 60,
                    DefaultRefreshTokenExpirationAfterInDays = 7
                }),
                NullLogger<TokenService>.Instance);
        }

        [TestCleanup]
        public void Cleanup() => _rsa.Dispose();

        [TestMethod]
        public async Task GenerateInternalTokenForSessionAsync_RegularUser_IssuesUserScopeWithoutRoleAndWithAccountClaims()
        {
            var account = Account();
            _accountRepositoryMock.Setup(x => x.GetByInternalIdAsync(account.Id.ToString())).ReturnsAsync(account);

            var result = await _sut.GenerateInternalTokenForSessionAsync(account.Id.ToString());

            result.IsSuccess.Should().BeTrue();
            JwtSecurityToken jwt = Read(result.Value.Token);
            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "user");
            jwt.Claims.Should().Contain(c => c.Type == "login" && c.Value == "johndoe");
            jwt.Claims.Should().Contain(c => c.Type == "avatar_url" && c.Value == "https://avatars.example/1");
            jwt.Claims.Should().NotContain(c => c.Type == "role");
        }

        [TestMethod]
        public async Task GenerateInternalTokenForSessionAsync_UnknownAccount_Fails()
        {
            _accountRepositoryMock.Setup(x => x.GetByInternalIdAsync(It.IsAny<string>()))
                .ReturnsAsync((AccountInformation)null!);

            var result = await _sut.GenerateInternalTokenForSessionAsync("user-1");

            result.IsFailure.Should().BeTrue();
        }

        [TestMethod]
        public async Task GenerateInternalTokenForSessionAsync_AdminSession_IssuesAdminScopeAndRoleWithoutAccountLookup()
        {
            var result = await _sut.GenerateInternalTokenForSessionAsync(AdminSession.UserId);

            result.IsSuccess.Should().BeTrue();
            JwtSecurityToken jwt = Read(result.Value.Token);
            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "admin");
            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
            _accountRepositoryMock.Verify(x => x.GetByInternalIdAsync(It.IsAny<string>()), Times.Never);
        }

        private static AccountInformation Account() => new()
        {
            Id = ObjectId.GenerateNewId(),
            IdentityProviderId = "1",
            Login = "johndoe",
            NodeId = "node",
            AvatarUrl = "https://avatars.example/1",
            Name = "John",
            Company = null,
            Email = "john@example.com",
            Bio = null,
            Location = null
        };

        private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);
    }
}
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    [TestClass]
    public class TokenServiceRefreshTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

        private Mock<IRefreshTokenRepository> _repositoryMock = null!;
        private RSA _rsa = null!;
        private TokenService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IRefreshTokenRepository>();
            _rsa = RSA.Create(2048);

            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);

            _sut = new TokenService(
                _rsa,
                new Mock<IIdentityProviderServiceFactory>().Object,
                timeProvider.Object,
                _repositoryMock.Object,
                new Mock<IStateRepository>().Object,
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
        public async Task RefreshInternalTokenAsync_RegularUser_IssuesUserScopeWithoutRole()
        {
            ArrangeToken("plain", "user-1");

            var result = await _sut.RefreshInternalTokenAsync(Token("plain"));

            JwtSecurityToken jwt = Read(result.Value.Token);
            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "user");
            jwt.Claims.Should().NotContain(c => c.Type == "role");
        }

        [TestMethod]
        public async Task RefreshInternalTokenAsync_AdminSession_IssuesAdminScopeAndRole()
        {
            ArrangeToken("plain", "admin");

            var result = await _sut.RefreshInternalTokenAsync(Token("plain"));

            JwtSecurityToken jwt = Read(result.Value.Token);
            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "admin");
            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
        }

        private void ArrangeToken(string token, string userId)
        {
            _repositoryMock.Setup(x => x.GetByTokenHashAsync(RefreshTokenHasher.Hash(token)))
                .ReturnsAsync(new Domain.Entities.RefreshToken
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId(),
                    TokenHash = RefreshTokenHasher.Hash(token),
                    UserId = userId,
                    ExpiresAfter = Now.AddDays(1),
                });
        }

        private static Values.RefreshToken Token(string token) => new()
        {
            Token = token,
            ExpiresAfter = Now.AddDays(1)
        };

        private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);
    }
}
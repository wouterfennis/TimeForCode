using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    [TestClass]
    public class TokenServiceSessionTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

        private RSA _rsa = null!;
        private TokenService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _rsa = RSA.Create(2048);

            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);

            _sut = new TokenService(
                _rsa,
                new Mock<IIdentityProviderServiceFactory>().Object,
                timeProvider.Object,
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
        public void GenerateInternalTokenForSession_RegularUser_IssuesUserScopeWithoutRole()
        {
            JwtSecurityToken jwt = Read(_sut.GenerateInternalTokenForSession("user-1").Token);

            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "user");
            jwt.Claims.Should().NotContain(c => c.Type == "role");
        }

        [TestMethod]
        public void GenerateInternalTokenForSession_AdminSession_IssuesAdminScopeAndRole()
        {
            JwtSecurityToken jwt = Read(_sut.GenerateInternalTokenForSession(AdminSession.UserId).Token);

            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "admin");
            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
        }

        private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);
    }
}
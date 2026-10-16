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
    public class TokenServiceTests
    {
        private static readonly ObjectId AccountId = ObjectId.GenerateNewId();
        private Mock<IAccountInformationRepository> _accountRepositoryMock = null!;
        private TokenService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _accountRepositoryMock = new Mock<IAccountInformationRepository>();

            _sut = new TokenService(
                RSA.Create(2048),
                new Mock<IIdentityProviderServiceFactory>().Object,
                TimeProvider.System,
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

        private static AccountInformation Account() => new()
        {
            Id = AccountId,
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

        [TestMethod]
        public void GenerateInternalToken_ForAccount_ContainsLoginAndAvatarClaims()
        {
            var token = _sut.GenerateInternalToken(Account());

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);
            jwt.Subject.Should().Be(AccountId.ToString());
            jwt.Claims.Single(c => c.Type == "scope").Value.Should().Be("user");
            jwt.Claims.Single(c => c.Type == "login").Value.Should().Be("octocat");
            jwt.Claims.Single(c => c.Type == "avatar_url").Value.Should().Be("https://avatars.githubusercontent.com/u/42");
        }

    }
}
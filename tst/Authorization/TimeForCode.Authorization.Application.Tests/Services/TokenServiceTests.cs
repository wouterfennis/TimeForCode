using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    [TestClass]
    public class TokenServiceTests
    {
        private static readonly ObjectId AccountId = ObjectId.GenerateNewId();
        private Mock<IAccountInformationRepository> _accountRepositoryMock = null!;
        private Mock<IIdentityProviderServiceFactory> _factoryMock = null!;
        private SigningKeySet _signingKeys = null!;
        private TokenService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _accountRepositoryMock = new Mock<IAccountInformationRepository>();
            _factoryMock = new Mock<IIdentityProviderServiceFactory>();

            _signingKeys = new SigningKeySet(RSA.Create(2048));
            _sut = new TokenService(
                _signingKeys,
                _factoryMock.Object,
                TimeProvider.System,
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
        public void GenerateInternalToken_ForAccount_HeaderNamesTheCurrentSigningKey()
        {
            var token = _sut.GenerateInternalToken(Account());

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);
            jwt.Header.Kid.Should().Be(_signingKeys.Current.KeyId);
            jwt.Header.Alg.Should().Be("RS256");
        }

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

        [TestMethod]
        public void GenerateTestAdminToken_ContainsAdminClaimsAndTestMarker()
        {
            var token = _sut.GenerateTestAdminToken();

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);
            jwt.Subject.Should().Be("admin");
            jwt.Claims.Single(c => c.Type == "scope").Value.Should().Be("admin");
            jwt.Claims.Single(c => c.Type == "role").Value.Should().Be("admin");
            jwt.Claims.Single(c => c.Type == "amr").Value.Should().Be("test");
        }

        [TestMethod]
        public void GenerateTestAdminToken_ExpiresSoonerThanARegularToken()
        {
            var token = _sut.GenerateTestAdminToken();

            token.ExpiresAfter.Should().BeBefore(DateTimeOffset.UtcNow.AddMinutes(16));
        }

        [TestMethod]
        public void GenerateInternalToken_ForAdminSession_HasNoTestMarker()
        {
            var token = _sut.GenerateInternalToken("admin", "admin", "admin");

            new JwtSecurityTokenHandler().ReadJwtToken(token.Token).Claims.Should().NotContain(c => c.Type == "amr");
        }

        [TestMethod]
        public async Task GetAccessTokenFromExternalProviderAsync_ValidState_SendsTheStoredCodeVerifier()
        {
            var providerMock = new Mock<IIdentityProviderService>();
            providerMock.Setup(x => x.GetAccessTokenAsync("code", "verifier"))
                .ReturnsAsync(Result<GetAccessTokenResult>.Success(new GetAccessTokenResult { AccessToken = "external", Scope = "s", TokenType = "t" }));
            _factoryMock.Setup(x => x.GetIdentityProviderService(IdentityProvider.Github))
                .Returns(Result<IIdentityProviderService>.Success(providerMock.Object));
            var entry = new StateEntry
            {
                Id = ObjectId.GenerateNewId(),
                Key = "state",
                IdentityProvider = IdentityProvider.Github,
                RedirectUri = new Uri("https://localhost/"),
                CodeVerifier = "verifier",
                ExpiresAfter = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            var result = await _sut.GetAccessTokenFromExternalProviderAsync(entry, "code");

            result.IsSuccess.Should().BeTrue();
            result.Value.Token.Should().Be("external");
        }
    }
}
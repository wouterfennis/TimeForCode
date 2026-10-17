using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using System.Web;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Shared.Api.Authentication;
using IServiceProvider = System.IServiceProvider;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class RefreshSteps
    {
        private const string PlainRefreshToken = "token";

        private readonly IAuthClient _authClient;
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _provider;
        private readonly CookieContainer _cookieContainer;
        private readonly SessionContext _session;
        private TryVoid<ApiException<ProblemDetails>?>? _result = null;
        private Domain.Entities.RefreshToken? _storedRefreshToken;

        public RefreshSteps(IAuthClient authClient, HttpClient httpClient, IServiceProvider provider, CookieContainer cookieContainer, SessionContext session)
        {
            _authClient = authClient;
            _httpClient = httpClient;
            _provider = provider;
            _cookieContainer = cookieContainer;
            _session = session;
        }

        [When("The user calls the refresh token endpoint")]
        public async Task WhenTheUserCallsTheRefreshTokenEndpointAsync()
        {
            _result = await _authClient.TryRefreshAsync();
        }

        [When("The user calls the refresh token endpoint with a read-only request")]
        public async Task WhenTheUserCallsTheRefreshTokenEndpointWithAReadOnlyRequestAsync()
        {
            _session.ReadOnlyRequestResponse = await _httpClient.GetAsync("/api/v1/authentication/refresh");
        }

        [Then("The following refresh error message is returned: {string}")]
        public void ThenTheFollowingRefreshErrorMessageIsReturned(string errorMessage)
        {
            _result!.Exception.Should().NotBeNull();

            var problemDetails = _result!.Exception!.Result;
            problemDetails!.Detail.Should().NotBeNull();
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Given("The user has an expired refresh token")]
        public void GivenTheUserHasAnExpiredRefreshToken()
        {
            SeedRefreshToken(DateTime.UtcNow.AddHours(-1));
        }

        [Given("The user has a refresh token")]
        public void GivenTheUserHasARefreshToken()
        {
            SeedRefreshToken(DateTime.UtcNow.AddHours(1));
        }

        [Given("The user has already refreshed with that refresh token")]
        public void GivenTheUserHasAlreadyRefreshedWithThatRefreshToken()
        {
            _storedRefreshToken!.RotatedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        }

        [Then("The refresh token is revoked")]
        public void ThenTheRefreshTokenIsRevoked()
        {
            var refreshTokenCookie = _cookieContainer.GetAllCookies()[CookieConstants.RefreshTokenKey];
            refreshTokenCookie.Should().BeNull();
        }

        [Then("The access token is revoked")]
        public void ThenTheAccessTokenIsRevoked()
        {
            var accessTokenCookie = _cookieContainer.GetAllCookies()[CookieConstants.TokenKey];
            accessTokenCookie.Should().BeNull();
        }

        [Then("A refresh token is returned")]
        public void ThenARefreshTokenIsReturned()
        {
            var refreshTokenCookie = _cookieContainer.GetAllCookies()[CookieConstants.RefreshTokenKey];
            refreshTokenCookie.Should().NotBeNull();
        }

        [Then("The refresh token is not stored in readable form")]
        public void ThenTheRefreshTokenIsNotStoredInReadableForm()
        {
            var repository = _provider.GetRequiredService<Mock<IRefreshTokenRepository>>();
            var newCookie = JsonSerializer.Deserialize<Values.RefreshToken>(
                HttpUtility.UrlDecode(_cookieContainer.GetAllCookies()[CookieConstants.RefreshTokenKey]!.Value))!;

            repository.Verify(x => x.CreateAsync(It.Is<Domain.Entities.RefreshToken>(t => t.TokenHash == RefreshTokenHasher.Hash(newCookie.Token))), Times.Once);
            repository.Verify(x => x.CreateAsync(It.Is<Domain.Entities.RefreshToken>(t => t.TokenHash == newCookie.Token)), Times.Never);
        }

        [Then("All other refresh tokens of the user are revoked")]
        public void ThenAllOtherRefreshTokensOfTheUserAreRevoked()
        {
            var repository = _provider.GetRequiredService<Mock<IRefreshTokenRepository>>();
            repository.Verify(x => x.DeleteByUserIdAsync(_session.UserId), Times.Once);
        }

        [Then("The access token contains the admin role and the admin scope")]
        public void ThenTheAccessTokenContainsTheAdminRoleAndTheAdminScope()
        {
            JwtSecurityToken jwt = ReadAccessTokenCookie();

            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "admin");
        }

        [Then("The access token contains the user scope")]
        public void ThenTheAccessTokenContainsTheUserScope()
        {
            JwtSecurityToken jwt = ReadAccessTokenCookie();

            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "user");
        }

        private JwtSecurityToken ReadAccessTokenCookie()
        {
            var cookie = _cookieContainer.GetAllCookies()[CookieConstants.TokenKey];
            cookie.Should().NotBeNull();

            var accessToken = JsonSerializer.Deserialize<AccessToken>(HttpUtility.UrlDecode(cookie!.Value))!;
            return new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        }

        private void SeedRefreshToken(DateTime expiresAfter)
        {
            var refreshToken = new Values.RefreshToken
            {
                Token = PlainRefreshToken,
                ExpiresAfter = expiresAfter,
            };

            var uri = new Uri("http://localhost:8084");
            var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(refreshToken));
            _cookieContainer.Add(uri, new Cookie(CookieConstants.RefreshTokenKey, cookieValue));

            _storedRefreshToken = new Domain.Entities.RefreshToken
            {
                ExpiresAfter = expiresAfter,
                Id = new ObjectId(),
                TokenHash = RefreshTokenHasher.Hash(PlainRefreshToken),
                UserId = _session.UserId
            };

            var repository = _provider.GetService<Mock<IRefreshTokenRepository>>()!;
            repository.Setup(x => x.GetByTokenHashAsync(RefreshTokenHasher.Hash(PlainRefreshToken)))
                .ReturnsAsync(_storedRefreshToken);
            repository.Setup(x => x.TryMarkRotatedAsync(It.IsAny<ObjectId>(), It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(true);
        }
    }
}
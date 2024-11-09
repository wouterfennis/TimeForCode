using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using System.Net;
using System.Text.Json;
using System.Web;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class RefreshSteps
    {
        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private readonly CookieContainer _cookieContainer;
        private TryVoid<ApiException<ProblemDetails>?>? _result = null;

        public RefreshSteps(IAuthClient authClient, IServiceProvider provider, CookieContainer cookieContainer)
        {
            _authClient = authClient;
            _provider = provider;
            _cookieContainer = cookieContainer;
        }

        [When("The user calls the refresh token endpoint")]
        public async Task WhenTheUserCallsTheRefreshTokenEndpointAsync()
        {
            _result = await _authClient.TryRefreshAsync();
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
            var refreshToken = new Values.RefreshToken
            {
                Token = "token",
                ExpiresAfter = DateTime.UtcNow.AddHours(-1),
            };

            var uri = new Uri("http://localhost");
            var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(refreshToken));
            _cookieContainer.Add(uri, new Cookie(CookieConstants.RefreshTokenKey, cookieValue));

            var expiredRefreshToken = new Domain.Entities.RefreshToken
            {
                ExpiresAfter = DateTime.UtcNow.AddHours(-1),
                Id = new ObjectId(),
                Token = refreshToken.Token,
                UserId = "id"
            };

            var repository = _provider.GetService<Mock<IRefreshTokenRepository>>()!;
            repository.Setup(x => x.GetByTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(expiredRefreshToken);
        }

        [Given("The user has a refresh token")]
        public void GivenTheUserHasARefreshToken()
        {
            var refreshToken = new Values.RefreshToken
            {
                Token = "token",
                ExpiresAfter = DateTime.UtcNow.AddHours(1),
            };

            var uri = new Uri("http://localhost");
            var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(refreshToken));
            _cookieContainer.Add(uri, new Cookie(CookieConstants.RefreshTokenKey, cookieValue));

            var expiredRefreshToken = new Domain.Entities.RefreshToken
            {
                ExpiresAfter = DateTime.UtcNow.AddHours(1),
                Id = new ObjectId(),
                Token = refreshToken.Token,
                UserId = "id"
            };

            var repository = _provider.GetService<Mock<IRefreshTokenRepository>>()!;
            repository.Setup(x => x.GetByTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(expiredRefreshToken);
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
    }
}
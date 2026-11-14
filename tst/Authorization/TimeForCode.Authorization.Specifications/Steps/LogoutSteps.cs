using AwesomeAssertions;
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
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class LogoutSteps
    {
        private readonly IAuthClient _authClient;
        private readonly CookieContainer _cookieContainer;
        private readonly IServiceProvider _provider;
        private readonly HttpClient _httpClient;
        private readonly SessionContext _session;
        private TryVoid<ApiException?>? _result;

        public LogoutSteps(IAuthClient authClient, CookieContainer cookieContainer, IServiceProvider provider, HttpClient httpClient, SessionContext session)
        {
            _httpClient = httpClient;
            _session = session;
            _authClient = authClient;
            _cookieContainer = cookieContainer;
            _provider = provider;
        }

        [Given("The user has not logged in at the time for code platform")]
        public void GivenTheUserHasNotLoggedInAtTheTimeForCodePlatform()
        {
            // The user holds no refresh token; sign-out is still confirmed.
            _session.RefreshToken = new RefreshToken { Token = "never-issued", ExpiresAfter = DateTimeOffset.UtcNow.AddDays(1) };
        }

        [Given("The user has an access token")]
        public void GivenTheUserHasAnAccessToken()
        {
            // The spec client sends this token as a bearer header for the calls that need a signed-in user.
            using var scope = _provider.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var accessToken = tokenService.GenerateInternalToken(new ObjectId().ToString(), "user", "user");

            var uri = new Uri("http://localhost:8084");
            var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(accessToken));
            _cookieContainer.Add(uri, new Cookie(CookieConstants.TokenKey, cookieValue));
        }

        [When("The user logs out from the external platform")]
        public async Task WhenTheUserLogsOutFromTheExternalPlatformAsync()
        {
            _result = await _authClient.TryLogoutAsync(_session.RefreshToken!);
        }

        [When("The user logs out with a read-only request")]
        public async Task WhenTheUserLogsOutWithAReadOnlyRequestAsync()
        {
            _session.ReadOnlyRequestResponse = await _httpClient.GetAsync("/api/v1/authentication/logout");
        }

        [Then("The logout is confirmed")]
        public void ThenTheLogoutIsConfirmed()
        {
            _result!.Exception.Should().BeNull();
        }

        [Then("The refresh token of the user is revoked")]
        public void ThenTheRefreshTokenOfTheUserIsRevoked()
        {
            var repository = _provider.GetRequiredService<Mock<IRefreshTokenRepository>>();

            repository.Verify(x => x.DeleteAsync(It.Is<Domain.Entities.RefreshToken>(t => t.TokenHash == RefreshTokenHasher.Hash(_session.RefreshToken!.Token))), Times.Once);
        }
    }
}
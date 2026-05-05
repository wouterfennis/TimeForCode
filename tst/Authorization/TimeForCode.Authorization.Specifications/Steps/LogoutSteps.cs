using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Reqnroll;
using System.Net;
using System.Text.Json;
using System.Web;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class LogoutSteps
    {
        private readonly IAuthClient _authClient;
        private readonly CookieContainer _cookieContainer;
        private readonly IServiceProvider _provider;
        private TryVoid<ApiException?>? _result;

        public LogoutSteps(IAuthClient authClient, CookieContainer cookieContainer, IServiceProvider provider)
        {
            _authClient = authClient;
            _cookieContainer = cookieContainer;
            _provider = provider;
        }

        [Given("The user has not logged in at the time for code platform")]
        public static void GivenTheUserHasNotLoggedInAtTheTimeForCodePlatform()
        {
            // do nothing
        }

        [Given("The user has an access token")]
        public void GivenTheUserHasAnAccessToken()
        {
            using var scope = _provider.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var accessToken = tokenService.GenerateInternalToken(new ObjectId().ToString());

            var uri = new Uri("http://localhost:8083");
            var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(accessToken));
            _cookieContainer.Add(uri, new Cookie(CookieConstants.TokenKey, cookieValue));
        }

        [When("The user logs out from the external platform")]
        public async Task WhenTheUserLogsOutFromTheExternalPlatformAsync()
        {
            _result = await _authClient.TryLogoutAsync();
        }

        [Then("The logout is confirmed")]
        public static void ThenTheLogoutIsConfirmed()
        {
            // do nothing, no exceptions should have been thrown
        }
    }
}
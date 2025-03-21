using Reqnroll;
using System.Net;
using System.Text.Json;
using System.Web;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class LogoutSteps
    {
        private readonly IAuthClient _authClient;
        private readonly CookieContainer _cookieContainer;

        public LogoutSteps(IAuthClient authClient, CookieContainer cookieContainer)
        {
            _authClient = authClient;
            _cookieContainer = cookieContainer;
        }

        [Given("The user has not logged in at the time for code platform")]
        public static void GivenTheUserHasNotLoggedInAtTheTimeForCodePlatform()
        {
            // do nothing
        }

        [Given("The user has an access token")]
        public void GivenTheUserHasAnAccessToken()
        {
            var accessToken = new Values.AccessToken
            {
                Token = "token",
                ExpiresAfter = DateTime.UtcNow.AddHours(1),
            };

            var uri = new Uri("http://localhost:8081");
            var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(accessToken));
            _cookieContainer.Add(uri, new Cookie(CookieConstants.TokenKey, cookieValue));
        }

        [When("The user logs out from the external platform")]
        public async Task WhenTheUserLogsOutFromTheExternalPlatformAsync()
        {
            await _authClient.TryLogoutAsync("http://localhost:8081");
        }

        [Then("The logout is confirmed")]
        public static void ThenTheLogoutIsConfirmed()
        {
            // do nothing, no exceptions should have been thrown
        }
    }
}
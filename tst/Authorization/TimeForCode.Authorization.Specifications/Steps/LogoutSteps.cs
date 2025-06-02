using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Reqnroll;
using System.Net;
using System.Text.Json;
using System.Web;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class LogoutSteps
    {
        private readonly IAuthClient _authClient;
        private readonly CookieContainer _cookieContainer;
        private TryVoid<ApiException?>? _result;

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
            var accessToken = new AccessToken
            {
                Token = "token",
                ExpiresAfter = DateTime.UtcNow.AddHours(1),
            };

            var uri = new Uri("http://localhost:8083");
            var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(accessToken));
            _cookieContainer.Add(uri, new Cookie(CookieConstants.TokenKey, cookieValue));
        }

        [When("The user logs out from the external platform")]
        public async Task WhenTheUserLogsOutFromTheExternalPlatformAsync()
        {
            _result = await _authClient.TryLogoutAsync(new Uri("http://localhost:8083"));
        }

        [When("The user logs out from the external platform with invalid redirect url")]
        public async Task WhenTheUserLogsOutFromTheExternalPlatformWithInvalidRedirectUrlAsync()
        {
            _result = await _authClient.TryLogoutAsync(new Uri("http://invalid-uri.com"));
        }

        [Then("The user is informed the logout redirect uri is rejected")]
        public void ThenTheUserIsInformedTheLogoutRedirectUriIsRejected()
        {
            _result.Should().NotBeNull();
            _result!.Exception.Should().NotBeNull();
            _result!.Exception!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Then("The logout is confirmed")]
        public static void ThenTheLogoutIsConfirmed()
        {
            // do nothing, no exceptions should have been thrown
        }
    }
}
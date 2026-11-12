using AwesomeAssertions;
using Reqnroll;
using System.Net;
using System.Text.Json;
using TimeForCode.Frontend.Bff.Specifications.Mocking;
using TimeForCode.Shared.Live;

namespace TimeForCode.Frontend.Bff.Specifications.Steps
{
    /// <summary>
    /// Sign-in, profile and sign-out against the compose stack (<c>@live</c>): the website, the Authorization API behind it
    /// and the identity provider mock that stands in for GitHub.
    /// </summary>
    [Binding]
    public sealed class LiveSignInSteps(Browser browser)
    {
        private const string LoginPath = "/api/auth/v1/authentication/login";
        private const string AuthRoutePrefix = "api/auth/";

        private HttpResponseMessage? _response;
        private HttpResponseMessage? _callback;
        private HttpResponseMessage? _landing;
        private string? _refreshCookieBeforeSignOut;

        [Given("The user has an account at the external platform")]
        public void GivenTheUserHasAnAccountAtTheExternalPlatform()
        {
            // The identity provider mock always has its default user.
        }

        [When("The user signs in on the time for code website")]
        public async Task WhenTheUserSignsInOnTheWebsite()
        {
            _response = await browser.GetAsync(LoginUrl());
        }

        [Then("The user is redirected to the external platform")]
        public void ThenTheUserIsRedirectedToTheExternalPlatform()
        {
            _response!.StatusCode.Should().Be(HttpStatusCode.Found);
            Uri location = _response.Headers.Location!;
            location.IsAbsoluteUri.Should().BeTrue();
            location.GetLeftPart(UriPartial.Authority).Should().NotBe(browser.BaseAddress.GetLeftPart(UriPartial.Authority));
            location.AbsolutePath.Should().Contain("authorize");
        }

        [Then("The external platform is asked to return the user to the time for code website")]
        public void ThenTheExternalPlatformIsAskedToReturnTheUserToTheWebsite()
        {
            string? redirectUri = System.Web.HttpUtility.ParseQueryString(_response!.Headers.Location!.Query)["redirect_uri"];

            redirectUri.Should().StartWith(new Uri(browser.BaseAddress, AuthRoutePrefix).AbsoluteUri);
        }

        [Given("The user has signed in on the time for code website")]
        [When("The user completes the sign-in on the time for code website")]
        public async Task WhenTheUserCompletesTheSignIn()
        {
            HttpResponseMessage login = await browser.GetAsync(LoginUrl());
            Uri identityProvider = login.Headers.Location!;
            HttpResponseMessage formResponse = await browser.GetAsync(identityProvider.AbsoluteUri);
            string form = await formResponse.Content.ReadAsStringAsync();

            using var confirm = new HttpRequestMessage(HttpMethod.Post, new Uri(identityProvider, "/login/oauth/confirm"))
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["state"] = LiveAuthentication.ExtractInputValue(form, "state"),
                    ["redirect_uri"] = LiveAuthentication.ExtractInputValue(form, "redirect_uri"),
                    ["client_id"] = LiveAuthentication.ExtractInputValue(form, "client_id"),
                    ["scope"] = LiveAuthentication.ExtractInputValue(form, "scope"),
                }),
            };
            HttpResponseMessage confirmed = await browser.SendAsync(confirm);

            _callback = await browser.GetAsync(confirmed.Headers.Location!.AbsoluteUri);
            _callback.StatusCode.Should().Be(HttpStatusCode.Found, await _callback.Content.ReadAsStringAsync());
            _landing = await browser.GetAsync(new Uri(browser.BaseAddress, _callback.Headers.Location!).AbsoluteUri);
        }

        [Then("The user is back on the time for code website")]
        public void ThenTheUserIsBackOnTheWebsite()
        {
            new Uri(browser.BaseAddress, _callback!.Headers.Location!).GetLeftPart(UriPartial.Authority)
                .Should().Be(browser.BaseAddress.GetLeftPart(UriPartial.Authority));
            _landing!.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Then("The token cookies cannot be read by the website's scripts")]
        public void ThenTheTokenCookiesCannotBeReadByScripts()
        {
            foreach (string name in new[] { "AccessToken", "RefreshToken" })
            {
                browser.SetCookieHeaders.Should().ContainSingle(header => header.StartsWith(name + "=", StringComparison.Ordinal))
                    .Which.Should().ContainEquivalentOf("httponly");
            }
        }

        [Then("No token appears in the address or in any answer the website reads")]
        public void ThenNoTokenAppearsInTheAddressOrInAnyAnswer()
        {
            foreach (string token in new[] { TokenIn("AccessToken"), TokenIn("RefreshToken") })
            {
                browser.SeenAddresses.Should().NotContain(address => address.Contains(token));
                browser.SeenBodies.Should().NotContain(body => body.Contains(token));
            }
        }

        [When("The user requests their profile through the time for code website")]
        public async Task WhenTheUserRequestsTheirProfileThroughTheWebsite()
        {
            _response = await browser.GetAsync("/api/auth/v1/user");
        }

        [Then("The profile is returned with name, login, avatar, email, company, bio, and location")]
        public async Task ThenTheProfileIsReturned()
        {
            string body = await _response!.Content.ReadAsStringAsync();
            _response.StatusCode.Should().Be(HttpStatusCode.OK, body);
            JsonElement profile = JsonDocument.Parse(body).RootElement;
            foreach (string field in new[] { "name", "login", "avatarUrl" })
            {
                profile.GetProperty(field).GetString().Should().NotBeNullOrEmpty(field);
            }

            foreach (string field in new[] { "email", "company", "bio", "location" })
            {
                profile.TryGetProperty(field, out _).Should().BeTrue(field);
            }
        }

        [When("The user signs out on the time for code website")]
        public async Task WhenTheUserSignsOutOnTheWebsite()
        {
            _refreshCookieBeforeSignOut = browser.Cookies["RefreshToken"];
            using HttpRequestMessage logout = Browser.StateChanging(HttpMethod.Post, "/api/auth/v1/authentication/logout");
            _response = await browser.SendAsync(logout);
            _response.IsSuccessStatusCode.Should().BeTrue(await _response.Content.ReadAsStringAsync());
        }

        [Then("The refresh token is revoked")]
        public async Task ThenTheRefreshTokenIsRevoked()
        {
            using HttpRequestMessage refresh = Browser.StateChanging(HttpMethod.Post, "/api/auth/v1/authentication/refresh");
            refresh.Headers.Add("Cookie", "RefreshToken=" + _refreshCookieBeforeSignOut);

            using HttpResponseMessage response = await browser.SendAsync(refresh);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Then("The user has no token cookies left")]
        public void ThenTheUserHasNoTokenCookiesLeft()
        {
            browser.Cookies.Should().NotContainKey("AccessToken").And.NotContainKey("RefreshToken");
        }

        private string LoginUrl() =>
            $"{LoginPath}?IdentityProvider=Github&RedirectUri={Uri.EscapeDataString(browser.BaseAddress.AbsoluteUri)}";

        // The cookies hold JSON with the token in "Token"; a cookie that is not JSON is the token itself.
        private string TokenIn(string cookie)
        {
            string value = Uri.UnescapeDataString(browser.Cookies[cookie]);
            try
            {
                return JsonDocument.Parse(value).RootElement.GetProperty("Token").GetString()!;
            }
            catch (JsonException)
            {
                return value;
            }
        }
    }
}
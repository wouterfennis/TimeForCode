using AwesomeAssertions;
using Reqnroll;
using System.Net;
using System.Text.Json;
using TimeForCode.Frontend.Bff.Specifications.Mocking;

namespace TimeForCode.Frontend.Bff.Specifications.Steps
{
    [Binding]
    public sealed class SessionSteps(Browser browser)
    {
        private const string SessionPath = "/api/session";
        private const string AccessTokenCookie = "AccessToken";

        private string? _accessToken;
        private HttpResponseMessage? _response;
        private string _body = string.Empty;

        [Given("^The user has (no|an expired|a malformed) access token$")]
        public void GivenTheUserHasAnUnusableAccessToken(string kind)
        {
            switch (kind)
            {
                case "an expired":
                    // Past the 5 minutes of clock skew that the BFF, like the APIs, allows.
                    browser.SetCookie(AccessTokenCookie, TestTokens.CookieValue(TestTokens.User(DateTime.UtcNow.AddMinutes(-10))));
                    break;
                case "a malformed":
                    browser.SetCookie(AccessTokenCookie, TestTokens.CookieValue("not-a-jwt"));
                    break;
            }
        }

        [Given("The user has an access token")]
        public void GivenTheUserHasAnAccessToken() => UseToken(TestTokens.User());

        [Given("The user has an administrator access token")]
        public void GivenTheUserHasAnAdministratorAccessToken() => UseToken(TestTokens.Admin());

        [When("The time for code website checks who is signed in")]
        public Task WhenTheWebsiteChecksWhoIsSignedIn() => CheckSessionAsync();

        [Then("The time for code website learns that the user is signed in")]
        public async Task ThenTheWebsiteLearnsThatTheUserIsSignedIn()
        {
            await CheckSessionAsync();
            Session().GetProperty("signedIn").GetBoolean().Should().BeTrue();
        }

        [Then("The time for code website learns that nobody is signed in")]
        public async Task ThenTheWebsiteLearnsThatNobodyIsSignedIn()
        {
            if (_response is null)
            {
                await CheckSessionAsync();
            }

            Session().GetProperty("signedIn").GetBoolean().Should().BeFalse();
        }

        [Then("The time for code website learns the login and avatar of the user")]
        public void ThenTheWebsiteLearnsTheLoginAndAvatarOfTheUser()
        {
            JsonElement session = Session();
            session.GetProperty("signedIn").GetBoolean().Should().BeTrue();
            session.GetProperty("login").GetString().Should().Be(TestTokens.Login);
            session.GetProperty("avatarUrl").GetString().Should().Be(TestTokens.AvatarUrl);
            session.GetProperty("isAdmin").GetBoolean().Should().BeFalse();
        }

        [Then("The time for code website learns that the user is an administrator")]
        public void ThenTheWebsiteLearnsThatTheUserIsAnAdministrator()
        {
            JsonElement session = Session();
            session.GetProperty("signedIn").GetBoolean().Should().BeTrue();
            session.GetProperty("isAdmin").GetBoolean().Should().BeTrue();
        }

        [Then("The answer contains no token")]
        public void ThenTheAnswerContainsNoToken()
        {
            _body.Should().NotContain(_accessToken!);
            _body.Should().NotContainEquivalentOf("token");
        }

        private void UseToken(string token)
        {
            _accessToken = token;
            browser.SetCookie(AccessTokenCookie, TestTokens.CookieValue(token));
        }

        private async Task CheckSessionAsync()
        {
            _response = await browser.GetAsync(SessionPath);
            _body = await _response.Content.ReadAsStringAsync();
        }

        private JsonElement Session()
        {
            _response!.StatusCode.Should().Be(HttpStatusCode.OK, _body);
            return JsonDocument.Parse(_body).RootElement;
        }
    }
}
using AwesomeAssertions;
using Reqnroll;
using System.Net;
using System.Text.Json;
using TimeForCode.Frontend.Bff.Specifications.Mocking;

namespace TimeForCode.Frontend.Bff.Specifications.Steps
{
    [Binding]
    public sealed class SessionSteps : IDisposable
    {
        private const string SessionPath = "/api/session";

        private readonly HttpClient _browser = Hooks.CreateInProcessBrowser();
        private string? _accessToken;
        private string? _cookie;
        private HttpResponseMessage? _response;
        private string _body = string.Empty;

        [Given("^The user has (no|an expired|a malformed) access token$")]
        public void GivenTheUserHasAnUnusableAccessToken(string kind)
        {
            _cookie = kind switch
            {
                // Past the 5 minutes of clock skew that the BFF, like the APIs, allows.
                "an expired" => "AccessToken=" + TestTokens.CookieValue(TestTokens.User(DateTime.UtcNow.AddMinutes(-10))),
                "a malformed" => "AccessToken=" + TestTokens.CookieValue("not-a-jwt"),
                _ => null,
            };
        }

        [Given("The user has an access token")]
        public void GivenTheUserHasAnAccessToken() => UseToken(TestTokens.User());

        [Given("The user has an administrator access token")]
        public void GivenTheUserHasAnAdministratorAccessToken() => UseToken(TestTokens.Admin());

        [When("The time for code website checks who is signed in")]
        public async Task WhenTheWebsiteChecksWhoIsSignedIn()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, SessionPath);
            if (_cookie is not null)
            {
                request.Headers.Add("Cookie", _cookie);
            }

            _response = await _browser.SendAsync(request);
            _body = await _response.Content.ReadAsStringAsync();
        }

        [Then("The time for code website learns that nobody is signed in")]
        public void ThenTheWebsiteLearnsThatNobodyIsSignedIn()
        {
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

        public void Dispose()
        {
            _response?.Dispose();
            _browser.Dispose();
        }

        private void UseToken(string token)
        {
            _accessToken = token;
            _cookie = "AccessToken=" + TestTokens.CookieValue(token);
        }

        private JsonElement Session()
        {
            _response!.StatusCode.Should().Be(HttpStatusCode.OK, _body);
            return JsonDocument.Parse(_body).RootElement;
        }
    }
}
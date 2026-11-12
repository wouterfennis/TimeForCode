using AwesomeAssertions;
using System.Net;
using System.Text.Json;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class BearerTokenTests
    {
        private BffFactory _factory = null!;
        private HttpClient _browser = null!;

        [TestInitialize]
        public void Initialize()
        {
            _factory = new BffFactory();
            _browser = _factory.CreateBrowser();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _browser.Dispose();
            _factory.Dispose();
        }

        [TestMethod]
        [DataRow("/api/donation/v1/Project")]
        [DataRow("/api/auth/v1/user")]
        public async Task Get_WithAccessTokenCookie_ForwardsTheTokenAsBearer(string path)
        {
            var cookie = JsonSerializer.Serialize(new AccessToken { Token = "jwt-value", ExpiresAfter = DateTimeOffset.UnixEpoch });
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("Cookie", $"AccessToken={Uri.EscapeDataString(cookie)}");

            await _browser.SendAsync(request);

            _factory.Upstream.Requests.Should().ContainSingle().Which.Headers["Authorization"].Should().Be("Bearer jwt-value");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("AccessToken=not-json")]
        public async Task Get_WithoutAUsableAccessTokenCookie_ForwardsNoAuthorizationHeader(string? cookie)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/v1/user");
            if (cookie is not null)
            {
                request.Headers.Add("Cookie", cookie);
            }

            await _browser.SendAsync(request);

            _factory.Upstream.Requests.Should().ContainSingle().Which.Headers.Should().NotContainKey("Authorization");
        }

        [TestMethod]
        public async Task Get_BrowserSendsItsOwnAuthorizationHeaderWithoutACookie_ForwardsNoAuthorizationHeader()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/v1/user");
            request.Headers.Add("Authorization", "Bearer forged-by-a-script");

            await _browser.SendAsync(request);

            _factory.Upstream.Requests.Should().ContainSingle().Which.Headers.Should().NotContainKey("Authorization");
        }

        [TestMethod]
        public async Task Get_BrowserSendsItsOwnAuthorizationHeaderAndACookie_ForwardsOnlyTheCookieToken()
        {
            var cookie = JsonSerializer.Serialize(new AccessToken { Token = "jwt-value", ExpiresAfter = DateTimeOffset.UnixEpoch });
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/v1/user");
            request.Headers.Add("Authorization", "Bearer forged-by-a-script");
            request.Headers.Add("Cookie", $"AccessToken={Uri.EscapeDataString(cookie)}");

            await _browser.SendAsync(request);

            _factory.Upstream.Requests.Should().ContainSingle().Which.Headers["Authorization"].Should().Be("Bearer jwt-value");
        }

        [TestMethod]
        public async Task Get_ApiAnswersUnauthorized_TheBrowserGetsUnauthorized()
        {
            _factory.Upstream.Respond = () => new HttpResponseMessage(HttpStatusCode.Unauthorized);

            var response = await _browser.GetAsync("/api/auth/v1/user");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
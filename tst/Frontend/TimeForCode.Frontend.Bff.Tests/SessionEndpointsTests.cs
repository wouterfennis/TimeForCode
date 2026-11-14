using AwesomeAssertions;
using System.Net;
using System.Text.Json;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class SessionEndpointsTests : BffTestBase
    {
        [TestMethod]
        public async Task Login_Always_RedirectsToTheAuthorizationApiWithThePkceChallengeAndTheCallbackOfThisHost()
        {
            using var response = await SendAsync(HttpMethod.Get, "/bff/login?returnUrl=/projects");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            var location = response.Headers.Location!;
            location.GetLeftPart(UriPartial.Path).Should().Be($"{BffFactory.PublicAuthorizationAddress}/api/v1/authentication/login");
            QueryValue(location, "IdentityProvider").Should().Be("Github");
            QueryValue(location, "RedirectUri").Should().Be("http://localhost/bff/callback");
            QueryValue(location, "CodeChallenge").Should().MatchRegex("^[A-Za-z0-9_-]{43}$");
        }

        [TestMethod]
        public async Task Login_Always_KeepsTheVerifierServerSideAndSetsOnlyAnOpaqueShortLivedCookie()
        {
            using var response = await SendAsync(HttpMethod.Get, "/bff/login");

            var cookie = SetCookies(response).Should().ContainSingle().Subject.ToLowerInvariant();
            cookie.Should().StartWith("__host-login=").And.Contain("httponly").And.Contain("secure").And.Contain("samesite=lax").And.Contain("path=/");
            cookie.Should().NotContain("domain=");
            Factory.Store.Sessions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Callback_CodeFromTheBrowserThatStartedTheLogin_OpensASessionAndReturnsToThePage()
        {
            using var start = await SendAsync(HttpMethod.Get, "/bff/login?returnUrl=/projects/42");
            var loginCookie = CookieValue(start, "__Host-login");

            using var response = await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location!.OriginalString.Should().Be("/projects/42");
            Factory.Store.Sessions.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Callback_Success_SessionCookieIsOpaqueHostPrefixedHttpOnlySecureAndStrict()
        {
            using var start = await SendAsync(HttpMethod.Get, "/bff/login");
            var loginCookie = CookieValue(start, "__Host-login");

            using var response = await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}");

            var session = SetCookies(response).Single(c => c.StartsWith("__Host-session=", StringComparison.Ordinal)).ToLowerInvariant();
            session.Should().Contain("httponly").And.Contain("secure").And.Contain("samesite=strict").And.Contain("path=/");
            session.Should().NotContain("domain=").And.NotContain("expires=");
            SetCookies(response).Should().Contain(c => c.StartsWith("__Host-login=;", StringComparison.Ordinal), "the login cookie is removed");
        }

        [TestMethod]
        public async Task Callback_Success_NoTokenIsInAnyCookieHeaderOrBody()
        {
            using var start = await SendAsync(HttpMethod.Get, "/bff/login");
            var loginCookie = CookieValue(start, "__Host-login");

            using var response = await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}");

            var everything = string.Join('\n', response.Headers.Select(h => $"{h.Key}: {string.Join(',', h.Value)}"))
                + await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            var session = Factory.Store.Sessions.Single();
            everything.Should().NotContain(session.AccessToken).And.NotContain(session.RefreshToken);
        }

        [TestMethod]
        public async Task Callback_Success_ExchangesTheCodeWithTheVerifierOfThisLogin()
        {
            using var start = await SendAsync(HttpMethod.Get, "/bff/login");
            var challenge = QueryValue(start.Headers.Location!, "CodeChallenge");
            var loginCookie = CookieValue(start, "__Host-login");

            await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}");

            var exchange = Factory.Api.Exchanges.Should().ContainSingle().Subject;
            Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(exchange.Verifier)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_').Should().Be(challenge);
        }

        [TestMethod]
        public async Task Callback_WithoutTheLoginCookie_IsAnotherBrowser_AndFailsWithoutCallingTheApi()
        {
            using var response = await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location!.OriginalString.Should().Be("/?signInError=1");
            SetCookies(response).Should().NotContain(c => c.StartsWith("__Host-session=", StringComparison.Ordinal) && !c.StartsWith("__Host-session=;", StringComparison.Ordinal));
            Factory.Api.Exchanges.Should().BeEmpty();
            Factory.Store.Sessions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Callback_ForgedCode_FailsAndOpensNoSession()
        {
            using var start = await SendAsync(HttpMethod.Get, "/bff/login");
            var loginCookie = CookieValue(start, "__Host-login");

            using var response = await SendAsync(HttpMethod.Get, "/bff/callback?code=forged", cookie: $"__Host-login={loginCookie}");

            response.Headers.Location!.OriginalString.Should().Be("/?signInError=1");
            Factory.Store.Sessions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Callback_WithoutACode_Fails()
        {
            using var start = await SendAsync(HttpMethod.Get, "/bff/login");
            var loginCookie = CookieValue(start, "__Host-login");

            using var response = await SendAsync(HttpMethod.Get, "/bff/callback", cookie: $"__Host-login={loginCookie}");

            response.Headers.Location!.OriginalString.Should().Be("/?signInError=1");
        }

        [TestMethod]
        public async Task Callback_TheSameLoginTwice_OpensOnlyOneSession()
        {
            using var start = await SendAsync(HttpMethod.Get, "/bff/login");
            var loginCookie = CookieValue(start, "__Host-login");
            await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}");

            using var again = await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}");

            again.Headers.Location!.OriginalString.Should().Be("/?signInError=1");
            Factory.Store.Sessions.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Callback_NewSignIn_GetsANewSessionIdEvenWhenTheBrowserSendsAnOldOne()
        {
            var old = await SignInAsync();
            using var start = await SendAsync(HttpMethod.Get, "/bff/login");
            var loginCookie = CookieValue(start, "__Host-login");

            using var response = await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}; __Host-session={old}");

            CookieValue(response, "__Host-session").Should().NotBe(old);
        }

        [TestMethod]
        public async Task Session_SignedIn_ReturnsWhoTheUserIsAndNoToken()
        {
            Factory.Api.IsNewUser = true;
            var sessionId = await SignInAsync();

            using var response = await SendAsync(HttpMethod.Get, "/bff/session", cookie: SessionCookie(sessionId));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            using var json = JsonDocument.Parse(body);
            json.RootElement.GetProperty("isSignedIn").GetBoolean().Should().BeTrue();
            json.RootElement.GetProperty("isNewUser").GetBoolean().Should().BeTrue();
            json.RootElement.GetProperty("userId").GetString().Should().Be("user-1");
            json.RootElement.GetProperty("login").GetString().Should().Be("octocat");
            var session = Factory.Store.Sessions.Single();
            body.Should().NotContain(session.AccessToken).And.NotContain(session.RefreshToken);
        }

        [TestMethod]
        public async Task Session_LongAfterTheFirstSignIn_NoLongerSaysTheUserIsNew()
        {
            Factory.Api.IsNewUser = true;
            var sessionId = await SignInAsync();
            Factory.Time.Advance(TimeSpan.FromMinutes(6));

            using var response = await SendAsync(HttpMethod.Get, "/bff/session", cookie: SessionCookie(sessionId));

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
            json.RootElement.GetProperty("isNewUser").GetBoolean().Should().BeFalse();
        }

        [TestMethod]
        public async Task Session_NotSignedIn_IsUnauthorized()
        {
            using var response = await SendAsync(HttpMethod.Get, "/bff/session");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task Session_ExpiredByIdleTimeout_IsUnauthorizedAndTheCookieIsCleared()
        {
            var sessionId = await SignInAsync();
            Factory.Time.Advance(TimeSpan.FromMinutes(31));

            using var response = await SendAsync(HttpMethod.Get, "/bff/session", cookie: SessionCookie(sessionId));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            SetCookies(response).Should().Contain(c => c.StartsWith("__Host-session=;", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task Session_Response_IsNeverCached()
        {
            using var response = await SendAsync(HttpMethod.Get, "/bff/session");

            response.Headers.CacheControl!.NoStore.Should().BeTrue();
        }

        [TestMethod]
        public async Task Logout_SignedIn_RevokesTheRefreshTokenEndsTheSessionAndClearsTheCookie()
        {
            var sessionId = await SignInAsync();

            using var response = await SendAsync(HttpMethod.Post, "/bff/logout", cookie: SessionCookie(sessionId), csrf: true, origin: "http://localhost");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.Api.Logouts.Should().Equal("refresh-1");
            Factory.Store.Sessions.Should().BeEmpty();
            SetCookies(response).Should().Contain(c => c.StartsWith("__Host-session=;", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task Logout_WithoutASession_IsStillNoContent()
        {
            using var response = await SendAsync(HttpMethod.Post, "/bff/logout", csrf: true);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task Logout_WithoutTheCsrfHeader_IsForbiddenAndKeepsTheSession()
        {
            var sessionId = await SignInAsync();

            using var response = await SendAsync(HttpMethod.Post, "/bff/logout", cookie: SessionCookie(sessionId));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Factory.Store.Sessions.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Logout_FromAnotherOrigin_IsForbiddenAndKeepsTheSession()
        {
            var sessionId = await SignInAsync();

            using var response = await SendAsync(HttpMethod.Post, "/bff/logout", cookie: SessionCookie(sessionId), csrf: true, origin: "https://evil.example");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Factory.Store.Sessions.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Logout_WithAGet_DoesNotSignTheUserOut()
        {
            var sessionId = await SignInAsync();

            using var response = await SendAsync(HttpMethod.Get, "/bff/logout", cookie: SessionCookie(sessionId));

            response.IsSuccessStatusCode.Should().BeFalse();
            Factory.Store.Sessions.Should().ContainSingle();
            Factory.Api.Logouts.Should().BeEmpty();
        }
    }
}
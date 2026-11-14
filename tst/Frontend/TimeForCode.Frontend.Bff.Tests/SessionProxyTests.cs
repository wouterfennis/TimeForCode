using AwesomeAssertions;
using System.Net;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class SessionProxyTests : BffTestBase
    {
        [TestMethod]
        [DataRow("/api/donation/v1/Project")]
        [DataRow("/api/auth/v1/user")]
        public async Task Get_WithASession_ForwardsTheAccessTokenAsBearer(string path)
        {
            var sessionId = await SignInAsync();

            await SendAsync(HttpMethod.Get, path, cookie: SessionCookie(sessionId));

            var forwarded = Factory.Upstream.Requests.Should().ContainSingle().Subject;
            forwarded.Headers["Authorization"].Should().Be($"Bearer {Factory.Store.Sessions.Single().AccessToken}");
        }

        [TestMethod]
        [DataRow("/api/donation/v1/Project")]
        [DataRow("/api/auth/v1/user")]
        public async Task Get_WithCookies_NeverForwardsACookieHeader(string path)
        {
            var sessionId = await SignInAsync();

            await SendAsync(HttpMethod.Get, path, cookie: $"{SessionCookie(sessionId)}; RefreshToken=leftover; Other=1");

            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers.Should().NotContainKey("Cookie");
        }

        [TestMethod]
        public async Task Get_WithoutASession_ForwardsNoAuthorizationHeader()
        {
            await SendAsync(HttpMethod.Get, "/api/donation/v1/Project");

            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers.Should().NotContainKey("Authorization");
        }

        [TestMethod]
        public async Task Get_BrowserSendsItsOwnAuthorizationHeader_ForwardsOnlyTheSessionToken()
        {
            var sessionId = await SignInAsync();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/v1/user");
            request.Headers.Add("Authorization", "Bearer forged-by-a-script");
            request.Headers.Add("Cookie", SessionCookie(sessionId));

            await Browser.SendAsync(request, TestContext.CancellationToken);

            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers["Authorization"]
                .Should().Be($"Bearer {Factory.Store.Sessions.Single().AccessToken}");
        }

        [TestMethod]
        public async Task Get_BrowserSendsItsOwnAuthorizationHeaderWithoutASession_ForwardsNoAuthorizationHeader()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/v1/user");
            request.Headers.Add("Authorization", "Bearer forged-by-a-script");

            await Browser.SendAsync(request, TestContext.CancellationToken);

            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers.Should().NotContainKey("Authorization");
        }

        [TestMethod]
        public async Task Get_AccessTokenExpired_RefreshesWithoutTheBrowserNoticing()
        {
            var sessionId = await SignInAsync();
            Factory.Time.Advance(TimeSpan.FromMinutes(16));

            var response = await SendAsync(HttpMethod.Get, "/api/donation/v1/Project", cookie: SessionCookie(sessionId));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Factory.Api.Refreshes.Should().Equal("refresh-1");
            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers["Authorization"].Should().Be($"Bearer {FakeAuthorizationApi.JwtFor(2)}");
            SetCookies(response).Should().BeEmpty("the session cookie does not change when tokens rotate");
        }

        [TestMethod]
        public async Task Get_ManyRequestsFindAnExpiredToken_RefreshOnlyOnce()
        {
            var sessionId = await SignInAsync();
            Factory.Time.Advance(TimeSpan.FromMinutes(16));
            Factory.Api.RefreshDelay = TimeSpan.FromMilliseconds(150);

            var responses = await Task.WhenAll(Enumerable.Range(0, 6)
                .Select(_ => SendAsync(HttpMethod.Get, "/api/donation/v1/Project", cookie: SessionCookie(sessionId))));

            responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
            Factory.Api.Refreshes.Should().HaveCount(1);
            Factory.Upstream.Requests.Select(r => r.Headers["Authorization"]).Should().OnlyContain(h => h == $"Bearer {FakeAuthorizationApi.JwtFor(2)}");
        }

        [TestMethod]
        public async Task Get_RefreshTokenRejected_ForwardsWithoutBearerAndEndsTheSession()
        {
            var sessionId = await SignInAsync();
            Factory.Time.Advance(TimeSpan.FromMinutes(16));
            Factory.Api.RejectRefresh = true;

            var response = await SendAsync(HttpMethod.Get, "/api/donation/v1/Project", cookie: SessionCookie(sessionId));

            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers.Should().NotContainKey("Authorization");
            Factory.Store.Sessions.Should().BeEmpty();
            SetCookies(response).Should().Contain(c => c.StartsWith("__Host-session=;", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task Get_AuthorizationApiUnreachableForTheRefresh_ForwardsWithoutBearerButKeepsTheSessionAndTheCookie()
        {
            var sessionId = await SignInAsync();
            Factory.Time.Advance(TimeSpan.FromMinutes(16));
            Factory.Api.RefreshFailure = new HttpRequestException("down");

            var response = await SendAsync(HttpMethod.Get, "/api/donation/v1/Project", cookie: SessionCookie(sessionId));

            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers.Should().NotContainKey("Authorization");
            Factory.Store.Sessions.Should().ContainSingle("an outage must not sign the user out");
            SetCookies(response).Should().BeEmpty("the session cookie stays");
        }

        [TestMethod]
        public async Task Get_SessionIdleTooLong_ForwardsWithoutBearer()
        {
            var sessionId = await SignInAsync();
            Factory.Time.Advance(TimeSpan.FromMinutes(31));

            await SendAsync(HttpMethod.Get, "/api/donation/v1/Project", cookie: SessionCookie(sessionId));

            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers.Should().NotContainKey("Authorization");
        }

        [TestMethod]
        [DataRow("/api/auth/v1/authentication/token")]
        [DataRow("/api/auth/v1/authentication/refresh")]
        [DataRow("/api/auth/v1/authentication/logout")]
        [DataRow("/api/auth/v1/admin-authentication/authentication")]
        public async Task Post_AuthorizationApiTokenEndpoints_AreNotReachableThroughTheBff(string path)
        {
            var sessionId = await SignInAsync();

            var response = await SendAsync(HttpMethod.Post, path, cookie: SessionCookie(sessionId), csrf: true);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            Factory.Upstream.Requests.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Post_FromAnotherOrigin_IsForbiddenAndNeverReachesTheApi()
        {
            var sessionId = await SignInAsync();

            var response = await SendAsync(HttpMethod.Post, "/api/donation/v1/Project", cookie: SessionCookie(sessionId), csrf: true, origin: "https://evil.example");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Factory.Upstream.Requests.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow("http://localhost")]
        [DataRow(null)]
        public async Task Post_FromTheSpaOrigin_IsForwarded(string? origin)
        {
            var sessionId = await SignInAsync();

            await SendAsync(HttpMethod.Post, "/api/donation/v1/Project", cookie: SessionCookie(sessionId), csrf: true, origin: origin);

            Factory.Upstream.Requests.Should().ContainSingle();
        }

        [TestMethod]
        public async Task EverySignInAndCall_LogsNoTokenCodeCookieValueOrSessionId()
        {
            var sessionId = await SignInAsync();
            await SendAsync(HttpMethod.Get, "/bff/session", cookie: SessionCookie(sessionId));
            await SendAsync(HttpMethod.Get, "/api/donation/v1/Project", cookie: SessionCookie(sessionId));
            Factory.Time.Advance(TimeSpan.FromMinutes(16));
            await SendAsync(HttpMethod.Get, "/api/donation/v1/Project", cookie: SessionCookie(sessionId));
            Factory.Api.RejectRefresh = true;
            Factory.Time.Advance(TimeSpan.FromMinutes(16));
            await SendAsync(HttpMethod.Get, "/api/donation/v1/Project", cookie: SessionCookie(sessionId));
            await SendAsync(HttpMethod.Post, "/bff/logout", cookie: SessionCookie(sessionId), csrf: true);

            var log = string.Join('\n', Factory.Logs.Lines);
            log.Should().NotBeEmpty("the capture is working");
            foreach (var secret in new[] { sessionId, "one-time-code", "refresh-1", "refresh-2", FakeAuthorizationApi.JwtFor(1), FakeAuthorizationApi.JwtFor(2), "Bearer ", "Set-Cookie" })
            {
                log.Should().NotContain(secret);
            }
        }
    }
}
using AwesomeAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>The admin passkey sign-in through the BFF: the browser only ever receives a ceremony id, options and the opaque session cookie.</summary>
    [TestClass]
    public sealed class AdminEndpointsTests : BffTestBase
    {
        private Task<HttpResponseMessage> PostAsync(string path, object? body, string? cookie = null, bool csrf = true, string? origin = SpaOrigin)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body ?? new { }) };
            if (cookie is not null)
            {
                request.Headers.Add("Cookie", cookie);
            }

            if (csrf)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            }

            if (origin is not null)
            {
                request.Headers.Add("Origin", origin);
            }

            return Browser.SendAsync(request, TestContext.CancellationToken);
        }

        [TestMethod]
        public async Task RegistrationOptions_CorrectBootstrapSecret_ReturnsTheCeremonyIdAndTheOptions()
        {
            Factory.Api.AdminRegistered = false;

            using var response = await PostAsync("/bff/admin/registration/options", new { bootstrapSecret = "bootstrap-secret" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
            json.RootElement.GetProperty("ceremonyId").GetString().Should().Be("registration-ceremony");
            json.RootElement.GetProperty("optionsJson").GetString().Should().Contain("challenge");
            SetCookies(response).Should().BeEmpty();
        }

        [TestMethod]
        public async Task RegistrationOptions_WrongSecretOrAlreadyClaimed_IsBadRequest()
        {
            Factory.Api.AdminRegistered = false;

            using var wrong = await PostAsync("/bff/admin/registration/options", new { bootstrapSecret = "wrong" });
            Factory.Api.AdminRegistered = true;
            using var claimed = await PostAsync("/bff/admin/registration/options", new { bootstrapSecret = "bootstrap-secret" });

            wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            claimed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task Registration_Success_OpensAnAdminSessionAndReturnsNoToken()
        {
            Factory.Api.AdminRegistered = false;

            using var response = await PostAsync("/bff/admin/registration", new { bootstrapSecret = "bootstrap-secret", credentialJson = "{}", ceremonyId = "registration-ceremony" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            var session = Factory.Store.Sessions.Should().ContainSingle().Subject;
            body.Should().NotContain(session.AccessToken).And.NotContain(session.RefreshToken);
            var cookie = SetCookies(response).Should().ContainSingle().Subject.ToLowerInvariant();
            cookie.Should().StartWith("__host-session=").And.Contain("httponly").And.Contain("secure").And.Contain("samesite=strict").And.Contain("path=/");
            using var json = JsonDocument.Parse(body);
            json.RootElement.GetProperty("isAdmin").GetBoolean().Should().BeTrue();
        }

        [TestMethod]
        public async Task Registration_Rejected_OpensNoSession()
        {
            Factory.Api.AdminRegistered = false;

            using var response = await PostAsync("/bff/admin/registration", new { bootstrapSecret = "wrong", credentialJson = "{}", ceremonyId = "registration-ceremony" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Factory.Store.Sessions.Should().BeEmpty();
            SetCookies(response).Should().BeEmpty();
        }

        [TestMethod]
        public async Task AuthenticationOptions_AnAdminIsRegistered_ReturnsTheCeremonyIdAndTheOptions()
        {
            using var response = await PostAsync("/bff/admin/authentication/options", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
            json.RootElement.GetProperty("ceremonyId").GetString().Should().Be("authentication-ceremony");
        }

        [TestMethod]
        public async Task AuthenticationOptions_NoAdminRegisteredYet_IsBadRequest()
        {
            Factory.Api.AdminRegistered = false;

            using var response = await PostAsync("/bff/admin/authentication/options", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task Authentication_Success_OpensAnAdminSessionWhoseTokenIsOnlyUsedAsBearer()
        {
            using var response = await PostAsync("/bff/admin/authentication", new { credentialJson = "{}", ceremonyId = "authentication-ceremony" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var sessionId = CookieValue(response, "__Host-session");

            await SendAsync(HttpMethod.Get, "/api/auth/v1/user", cookie: SessionCookie(sessionId));

            Factory.Upstream.Requests.Should().ContainSingle().Which.Headers["Authorization"]
                .Should().Be($"Bearer {Factory.Store.Sessions.Single().AccessToken}");
            Factory.Upstream.Requests.Single().Headers.Should().NotContainKey("Cookie");
        }

        [TestMethod]
        public async Task Authentication_AdminSession_IsReportedBySessionStatus()
        {
            using var response = await PostAsync("/bff/admin/authentication", new { credentialJson = "{}", ceremonyId = "authentication-ceremony" });
            var sessionId = CookieValue(response, "__Host-session");

            using var status = await SendAsync(HttpMethod.Get, "/bff/session", cookie: SessionCookie(sessionId));

            using var json = JsonDocument.Parse(await status.Content.ReadAsStringAsync(TestContext.CancellationToken));
            json.RootElement.GetProperty("isAdmin").GetBoolean().Should().BeTrue();
            json.RootElement.GetProperty("userId").GetString().Should().Be("admin");
        }

        [TestMethod]
        public async Task Authentication_WrongDevice_IsBadRequestWithoutASession()
        {
            using var response = await PostAsync("/bff/admin/authentication", new { credentialJson = "wrong-device", ceremonyId = "authentication-ceremony" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Factory.Store.Sessions.Should().BeEmpty();
            SetCookies(response).Should().BeEmpty();
        }

        [TestMethod]
        public async Task Authentication_AuthorizationApiIssuesATokenTheBffCannotRead_OpensNoSession()
        {
            Factory.Api.AdminTokenIsUnreadable = true;

            using var response = await PostAsync("/bff/admin/authentication", new { credentialJson = "{}", ceremonyId = "authentication-ceremony" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Factory.Store.Sessions.Should().BeEmpty();
            SetCookies(response).Should().BeEmpty();
        }

        [TestMethod]
        public async Task Authentication_TheSameCeremonyTwice_IsRefusedTheSecondTime()
        {
            using var first = await PostAsync("/bff/admin/authentication", new { credentialJson = "{}", ceremonyId = "authentication-ceremony" });

            using var replay = await PostAsync("/bff/admin/authentication", new { credentialJson = "{}", ceremonyId = "authentication-ceremony" });

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            replay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Factory.Store.Sessions.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Authentication_WithAnEarlierSession_DiscardsIt()
        {
            using var first = await PostAsync("/bff/admin/authentication", new { credentialJson = "{}", ceremonyId = "first" });
            var old = CookieValue(first, "__Host-session");

            using var second = await PostAsync("/bff/admin/authentication", new { credentialJson = "{}", ceremonyId = "second" }, cookie: SessionCookie(old));

            CookieValue(second, "__Host-session").Should().NotBe(old);
            Factory.Store.Sessions.Should().ContainSingle();
        }

        [TestMethod]
        [DataRow("/bff/admin/registration/options")]
        [DataRow("/bff/admin/registration")]
        [DataRow("/bff/admin/authentication/options")]
        [DataRow("/bff/admin/authentication")]
        public async Task AdminEndpoints_WithoutTheCsrfHeader_AreForbidden(string path)
        {
            using var response = await PostAsync(path, new { }, csrf: false);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Factory.Api.AdminAuthentications.Should().BeEmpty();
        }

        [TestMethod]
        public async Task AdminEndpoints_FromAnotherOrigin_AreForbidden()
        {
            using var response = await PostAsync("/bff/admin/authentication", new { credentialJson = "{}", ceremonyId = "x" }, origin: "https://evil.example");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Factory.Api.AdminAuthentications.Should().BeEmpty();
        }

        [TestMethod]
        public async Task AdminEndpoints_Responses_AreNeverCached()
        {
            using var response = await PostAsync("/bff/admin/authentication/options", null);

            response.Headers.CacheControl!.NoStore.Should().BeTrue();
        }
    }
}
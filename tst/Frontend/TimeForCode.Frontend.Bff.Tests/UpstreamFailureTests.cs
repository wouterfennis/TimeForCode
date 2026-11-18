using AwesomeAssertions;
using System.Net;
using System.Net.Http.Json;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>An outage of the Authorization API or an oversized body must end in a clean answer, never an empty 500 or a stack trace.</summary>
    [TestClass]
    public sealed class UpstreamFailureTests : BffTestBase
    {
        private async Task<HttpResponseMessage> PostAdminAsync(string path, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            // A browser sends the length of a JSON body; buffering makes the test client do the same instead of sending it chunked.
            await request.Content.LoadIntoBufferAsync(TestContext.CancellationToken);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Origin", SpaOrigin);

            return await Browser.SendAsync(request, TestContext.CancellationToken);
        }

        [TestMethod]
        public async Task Callback_AuthorizationApiIsDown_RedirectsToTheSignInFailedPage()
        {
            using var start = await SendAsync(HttpMethod.Get, "/bff/login?returnUrl=/projects/42");
            var loginCookie = CookieValue(start, "__Host-login");
            Factory.Api.ExchangeFailure = new HttpRequestException("The Authorization API is down.");

            using var response = await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location!.OriginalString.Should().Be("/?signInError=1");
            Factory.Store.Sessions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task AdminRegistrationOptions_AuthorizationApiIsDown_ReturnsAProblemDetailsWithoutTheReason()
        {
            Factory.Api.AdminFailure = new HttpRequestException("secret internal detail");

            using var response = await PostAdminAsync("/bff/admin/registration/options", new { bootstrapSecret = "bootstrap-secret" });

            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Should().NotContain("secret internal detail");
        }

        [TestMethod]
        public async Task AdminRegistrationOptions_BodyOverTheLimit_IsRefused()
        {
            using var response = await PostAdminAsync("/bff/admin/registration/options", new { bootstrapSecret = new string('x', 20_000) });

            response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }
    }
}
using System.Net;
using System.Web;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>A BFF in memory plus a browser that keeps no cookies of its own: each test sends the cookies it wants.</summary>
    public abstract class BffTestBase
    {
        protected const string SpaOrigin = "http://localhost";

        public TestContext TestContext { get; set; } = null!;

        internal BffFactory Factory { get; private set; } = null!;

        protected HttpClient Browser { get; private set; } = null!;

        [TestInitialize]
        public void InitializeBff()
        {
            Factory = new BffFactory();
            Browser = Factory.CreateBrowser();
        }

        [TestCleanup]
        public void CleanupBff()
        {
            Browser.Dispose();
            Factory.Dispose();
        }

        protected Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? cookie = null, bool csrf = false, string? origin = SpaOrigin)
        {
            var request = new HttpRequestMessage(method, path);
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

        /// <summary>Signs in the way a browser does: start the login, then come back from the Authorization API with a code.</summary>
        protected async Task<string> SignInAsync(string returnUrl = "/projects")
        {
            using var start = await SendAsync(HttpMethod.Get, $"/bff/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            var loginCookie = CookieValue(start, "__Host-login");

            using var callback = await SendAsync(HttpMethod.Get, "/bff/callback?code=one-time-code", cookie: $"__Host-login={loginCookie}");
            return CookieValue(callback, "__Host-session");
        }

        protected static string SessionCookie(string sessionId) => $"__Host-session={sessionId}";

        protected static string[] SetCookies(HttpResponseMessage response) =>
            response.Headers.TryGetValues("Set-Cookie", out var values) ? [.. values] : [];

        protected static string CookieValue(HttpResponseMessage response, string name)
        {
            var header = SetCookies(response).SingleOrDefault(c => c.StartsWith(name + "=", StringComparison.Ordinal));
            Assert.IsNotNull(header, $"the response should set the {name} cookie");
            return header![(name.Length + 1)..].Split(';')[0];
        }

        protected static string QueryValue(Uri uri, string name) => HttpUtility.ParseQueryString(uri.Query)[name]!;
    }

}
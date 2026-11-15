using AwesomeAssertions;
using System.Net;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class ForwardingTests
    {
        public TestContext TestContext { get; set; } = null!;

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
        [DataRow("/api/donation/v1/Project?pageSize=1", "http://donation-api:8080/api/v1/Project?pageSize=1")]
        [DataRow("/api/auth/v1/user", "http://authorization-api:8080/api/v1/user")]
        public async Task Get_ApiRoute_ReachesTheApiWithoutTheRoutePrefix(string path, string expectedUri)
        {
            await _browser.GetAsync(path, TestContext.CancellationToken);

            _factory.Upstream.Requests.Should().ContainSingle().Which.Uri.Should().Be(new Uri(expectedUri));
        }

        [TestMethod]
        public async Task Get_DonationRoute_ReturnsTheApiResponse()
        {
            var response = await _browser.GetAsync("/api/donation/v1/Project", TestContext.CancellationToken);

            (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Should().Be("{\"projects\":[]}");
        }

        [TestMethod]
        [DataRow("/api/donation/v1/Project")]
        [DataRow("/api/auth/v1/user")]
        public async Task Get_ApiRoute_IsNeverCached(string path)
        {
            var response = await _browser.GetAsync(path, TestContext.CancellationToken);

            response.Headers.CacheControl!.NoStore.Should().BeTrue();
        }

        [TestMethod]
        public async Task Get_ApiSetsACookie_TheBrowserNeverGetsIt()
        {
            _factory.Upstream.Respond = () =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Headers.Add("Set-Cookie", "AccessToken=value; path=/; secure; samesite=strict; httponly");
                return response;
            };

            var response = await _browser.GetAsync("/api/auth/v1/user", TestContext.CancellationToken);

            response.Headers.Contains("Set-Cookie").Should().BeFalse("the browser holds only the session cookie of the BFF");
        }

        [TestMethod]
        [DataRow("/api/donation/v1/Project", "http://donation-api:8080/api/v1/Project/42", "/api/donation/v1/Project/42")]
        [DataRow("/api/auth/v1/user", "http://authorization-api:8080/api/v1/user", "/api/auth/v1/user")]
        public async Task Get_ApiAnswersWithALocation_OnlyTheApisOwnPathsPointAtTheRoute(string path, string location, string expectedLocation)
        {
            _factory.Upstream.Respond = () =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.Redirect);
                response.Headers.Location = new Uri(location);
                return response;
            };

            var response = await _browser.GetAsync(path, TestContext.CancellationToken);

            response.Headers.Location!.OriginalString.Should().Be(expectedLocation);
        }
    }
}
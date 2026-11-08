using AwesomeAssertions;
using System.Net;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class CsrfTests
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
        [DataRow("POST")]
        [DataRow("PUT")]
        [DataRow("PATCH")]
        [DataRow("DELETE")]
        public async Task StateChangingRequest_WithoutTheCsrfHeader_IsForbidden(string method)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), "/api/donation/v1/Project");

            var response = await _browser.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        [DataRow("POST")]
        [DataRow("DELETE")]
        public async Task StateChangingRequest_WithoutTheCsrfHeader_NeverReachesTheApi(string method)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), "/api/auth/v1/authentication/logout");

            await _browser.SendAsync(request);

            _factory.Upstream.Requests.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow("POST")]
        [DataRow("PUT")]
        [DataRow("PATCH")]
        [DataRow("DELETE")]
        public async Task StateChangingRequest_WithTheCsrfHeader_IsForwarded(string method)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), "/api/donation/v1/Project");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            await _browser.SendAsync(request);

            _factory.Upstream.Requests.Should().ContainSingle().Which.Method.Method.Should().Be(method);
        }

        [TestMethod]
        [DataRow("GET")]
        [DataRow("HEAD")]
        public async Task SafeRequest_WithoutTheCsrfHeader_IsForwarded(string method)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), "/api/donation/v1/Project");

            await _browser.SendAsync(request);

            _factory.Upstream.Requests.Should().ContainSingle().Which.Method.Method.Should().Be(method);
        }
    }
}
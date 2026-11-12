using AwesomeAssertions;
using System.Net;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class StaticFilesTests
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
        [DataRow("/")]
        [DataRow("/projects/abc")]
        public async Task Get_SpaRoute_ReturnsIndexHtml(string path)
        {
            var response = await _browser.GetAsync(path);

            (await response.Content.ReadAsStringAsync()).Should().Contain("<div id=\"root\"></div>");
        }

        [TestMethod]
        [DataRow("/")]
        [DataRow("/projects/abc")]
        public async Task Get_SpaRoute_IsRevalidatedOnEveryLoad(string path)
        {
            var response = await _browser.GetAsync(path);

            response.Headers.CacheControl!.NoCache.Should().BeTrue();
        }

        [TestMethod]
        public async Task Get_BuiltAsset_IsCachedForAYear()
        {
            var response = await _browser.GetAsync("/assets/app.js");

            response.Headers.CacheControl!.ToString().Should().Be("public, max-age=31536000, immutable");
        }

        [TestMethod]
        public async Task Get_MissingAsset_ReturnsNotFound()
        {
            var response = await _browser.GetAsync("/assets/missing.js");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        [DataRow("/api/unknown/v1/x")]
        [DataRow("/api/donations/v1/Project")]
        public async Task Get_ApiPathWithoutARoute_ReturnsNotFoundInsteadOfTheSpa(string path)
        {
            var response = await _browser.GetAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
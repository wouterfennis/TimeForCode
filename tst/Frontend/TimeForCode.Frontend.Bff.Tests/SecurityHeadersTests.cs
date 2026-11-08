using AwesomeAssertions;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class SecurityHeadersTests
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
        [DataRow("/", "Content-Security-Policy", "default-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'")]
        [DataRow("/", "X-Content-Type-Options", "nosniff")]
        [DataRow("/", "Referrer-Policy", "strict-origin-when-cross-origin")]
        [DataRow("/assets/missing.js", "Content-Security-Policy", "default-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'")]
        [DataRow("/assets/missing.js", "X-Content-Type-Options", "nosniff")]
        [DataRow("/assets/missing.js", "Referrer-Policy", "strict-origin-when-cross-origin")]
        [DataRow("/api/donation/v1/Project", "Content-Security-Policy", "default-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'")]
        [DataRow("/api/donation/v1/Project", "X-Content-Type-Options", "nosniff")]
        [DataRow("/api/donation/v1/Project", "Referrer-Policy", "strict-origin-when-cross-origin")]
        public async Task Get_AnyResponse_CarriesSecurityHeader(string path, string header, string value)
        {
            var response = await _browser.GetAsync(path);

            response.Headers.GetValues(header).Should().ContainSingle().Which.Should().Be(value);
        }
    }
}
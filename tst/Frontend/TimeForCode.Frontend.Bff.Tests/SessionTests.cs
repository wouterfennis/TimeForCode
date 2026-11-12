using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class SessionTests
    {
        private const string Issuer = "http://authorization-api:8080";
        private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes("bff-tests-signing-key-of-at-least-32-bytes!"));

        public TestContext TestContext { get; set; } = null!;

        private BffFactory _factory = null!;
        private HttpClient _browser = null!;

        [TestInitialize]
        public void Initialize()
        {
            _factory = new BffFactory();
            _browser = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = null;
                    options.MetadataAddress = null!;
                    options.ConfigurationManager = null;
                    options.TokenValidationParameters.IssuerSigningKey = SigningKey;
                }))).CreateClient();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _browser.Dispose();
            _factory.Dispose();
        }

        [TestMethod]
        public async Task Get_ValidTokenOnlyInTheAuthorizationHeader_ReportsSignedOut()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/session");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ValidUserToken());

            var response = await _browser.SendAsync(request, TestContext.CancellationToken);

            (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Should().Contain("\"signedIn\":false");
        }

        [TestMethod]
        public async Task Get_Always_IsNotCached()
        {
            var response = await _browser.GetAsync("/api/session", TestContext.CancellationToken);

            response.Headers.CacheControl!.NoStore.Should().BeTrue();
        }

        private static string ValidUserToken() => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer,
            audience: Issuer,
            claims: [new Claim("sub", "user-1"), new Claim("scope", "user")],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)));
    }
}
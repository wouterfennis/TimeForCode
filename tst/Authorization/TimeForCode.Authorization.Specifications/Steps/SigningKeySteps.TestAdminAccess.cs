using FluentAssertions;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TimeForCode.Authorization.Specifications.TestBuilder;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Specifications.Steps
{
    internal sealed partial class SigningKeySteps
    {
        private const string TestAdminSecret = "test-admin-secret-for-specs";
        private const string TestAdminTokenPath = "/api/v1/test-admin-access/token";

        private TestSigningKey? _testAdminSigningKey;
        private HttpResponseMessage? _testAdminResponse;
        private string? _testAdminBody;

        [Given("The time for code platform has test admin access disabled")]
        public void GivenTheTimeForCodePlatformHasTestAdminAccessDisabled()
        {
            _settings["TestAdminAccess:Enabled"] = "false";
            _settings["TestAdminAccess:Secret"] = TestAdminSecret;
        }

        [Given("The time for code platform has test admin access enabled")]
        public void GivenTheTimeForCodePlatformHasTestAdminAccessEnabled()
        {
            EnableTestAdminAccess(TestAdminSecret);
        }

        [Given("The time for code platform has test admin access enabled without a secret")]
        public void GivenTheTimeForCodePlatformHasTestAdminAccessEnabledWithoutASecret()
        {
            EnableTestAdminAccess("");
        }

        [When("The user requests a test admin token with the test admin secret")]
        public async Task WhenTheUserRequestsATestAdminTokenWithTheTestAdminSecretAsync()
        {
            await RequestTestAdminTokenAsync(TestAdminSecret);
        }

        [When("The user requests a test admin token with a wrong secret")]
        public async Task WhenTheUserRequestsATestAdminTokenWithAWrongSecretAsync()
        {
            await RequestTestAdminTokenAsync("not-the-test-admin-secret");
        }

        [Then("The test admin token is not available")]
        public void ThenTheTestAdminTokenIsNotAvailable()
        {
            _testAdminResponse!.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Then("The test admin token is refused")]
        public void ThenTheTestAdminTokenIsRefused()
        {
            _testAdminResponse!.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Then("The test admin token grants the admin scope")]
        public void ThenTheTestAdminTokenGrantsTheAdminScope()
        {
            _testAdminResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

            ReadTestAdminToken().Claims.Should().Contain(c => c.Type == "scope" && c.Value == "admin");
        }

        [Then("The test admin token is marked as a test session")]
        public void ThenTheTestAdminTokenIsMarkedAsATestSession()
        {
            ReadTestAdminToken().Claims.Should().Contain(c => c.Type == "amr" && c.Value == "test");
        }

        [Then("The test admin token expires within 15 minutes")]
        public void ThenTheTestAdminTokenExpiresWithin15Minutes()
        {
            ReadTestAdminToken().ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(16));
        }

        [Then("No refresh token is issued with the test admin token")]
        public void ThenNoRefreshTokenIsIssuedWithTheTestAdminToken()
        {
            _testAdminBody.Should().NotContainEquivalentOf("refresh");
            _testAdminResponse!.Headers.Contains("Set-Cookie").Should().BeFalse();
        }

        [Then("The startup failure states that test admin access is not allowed")]
        public void ThenTheStartupFailureStatesThatTestAdminAccessIsNotAllowed()
        {
            _startupFailure!.Message.Should().Contain("TestAdminAccess:Enabled must not be true");
        }

        [Then("The startup failure states that a test admin secret is required")]
        public void ThenTheStartupFailureStatesThatATestAdminSecretIsRequired()
        {
            _startupFailure!.Message.Should().Contain("TestAdminAccess:Secret must be at least");
        }

        private JwtSecurityToken ReadTestAdminToken()
        {
            var accessToken = JsonSerializer.Deserialize<AccessToken>(_testAdminBody!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return new JwtSecurityTokenHandler().ReadJwtToken(accessToken!.Token);
        }

        private void EnableTestAdminAccess(string secret)
        {
            _testAdminSigningKey = new TestSigningKey();
            _settings["RsaOptions:Base64Certificate"] = _testAdminSigningKey.Base64Certificate;
            _settings["TestAdminAccess:Enabled"] = "true";
            _settings["TestAdminAccess:Secret"] = secret;
        }

        private async Task RequestTestAdminTokenAsync(string secret)
        {
            using var client = _factory!.CreateClient();
            _testAdminResponse = await client.PostAsJsonAsync(TestAdminTokenPath, new { Secret = secret });
            _testAdminBody = await _testAdminResponse.Content.ReadAsStringAsync();
        }
    }
}
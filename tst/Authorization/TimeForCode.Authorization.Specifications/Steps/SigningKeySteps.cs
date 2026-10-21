using FluentAssertions;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using TimeForCode.Authorization.Specifications.Mocking;
using TimeForCode.Authorization.Specifications.TestBuilder;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal sealed partial class SigningKeySteps : IDisposable
    {
        private const string JwksPath = "/.well-known/jwks.json";
        private const string Issuer = "http://localhost:8080";

        private readonly HttpClient _defaultClient;
        private readonly CookieContainer _cookieContainer;
        private readonly ScenarioInfrastructure _infrastructure;
        private readonly Dictionary<string, string?> _settings = new();
        private string _environment = "Development";
        private ConfiguredPlatformFactory? _factory;
        private HttpClient? _rotationClient;
        private Exception? _startupFailure;
        private TestSigningKey? _currentKey;
        private TestSigningKey? _previousKey;
        private string? _heldToken;
        private List<JwkResponse> _publishedKeys = [];

        public SigningKeySteps(HttpClient defaultClient, CookieContainer cookieContainer, ScenarioInfrastructure infrastructure)
        {
            _defaultClient = defaultClient;
            _cookieContainer = cookieContainer;
            _infrastructure = infrastructure;
        }

        [Given("The time for code platform is configured without a signing certificate")]
        public void GivenTheTimeForCodePlatformIsConfiguredWithoutASigningCertificate()
        {
            _settings["RsaOptions:Base64Certificate"] = "";
        }

        [Given("The time for code platform runs outside the Development environment")]
        public void GivenTheTimeForCodePlatformRunsOutsideTheDevelopmentEnvironment()
        {
            _environment = "Production";
        }

        [Given("The time for code platform runs in the {word} environment")]
        public void GivenTheTimeForCodePlatformRunsInTheEnvironment(string environment)
        {
            _environment = environment;
        }

        [Given("The time for code platform has a current and a previous signing key")]
        public void GivenTheTimeForCodePlatformHasACurrentAndAPreviousSigningKey()
        {
            _currentKey = new TestSigningKey();
            _previousKey = new TestSigningKey();
            _settings["RsaOptions:Base64Certificate"] = _currentKey.Base64Certificate;
            _settings["RsaOptions:PreviousBase64Certificate"] = _previousKey.Base64Certificate;

            _factory = new ConfiguredPlatformFactory(_infrastructure, _environment, _settings);
            _rotationClient = _factory.CreateClient();
        }

        [Given("The user holds an access token signed with the previous signing key")]
        public async Task GivenTheUserHoldsAnAccessTokenSignedWithThePreviousSigningKeyAsync()
        {
            _heldToken = await CreateTokenAsync(_previousKey!);
        }

        [Given("The user holds an access token signed with the current signing key")]
        public async Task GivenTheUserHoldsAnAccessTokenSignedWithTheCurrentSigningKeyAsync()
        {
            _heldToken = await CreateTokenAsync(_currentKey!);
        }

        [When("The time for code platform starts")]
        public void WhenTheTimeForCodePlatformStarts()
        {
            _factory = new ConfiguredPlatformFactory(_infrastructure, _environment, _settings);

            try
            {
                _ = _factory.Services;
            }
            catch (Exception exception)
            {
                _startupFailure = exception;
            }
        }

        [When("The user requests the published signing keys")]
        public async Task WhenTheUserRequestsThePublishedSigningKeysAsync()
        {
            _publishedKeys = await GetPublishedKeysAsync();
        }

        [Then("The time for code platform fails to start")]
        public void ThenTheTimeForCodePlatformFailsToStart()
        {
            _startupFailure.Should().NotBeNull();
        }

        [Then("The startup failure states that a signing certificate is required")]
        public void ThenTheStartupFailureStatesThatASigningCertificateIsRequired()
        {
            _startupFailure!.Message.Should().Contain("signing certificate is required");
        }

        [Then("The time for code platform starts successfully")]
        public void ThenTheTimeForCodePlatformStartsSuccessfully()
        {
            _startupFailure.Should().BeNull();
        }

        [Then("The issued access token names a signing key")]
        public void ThenTheIssuedAccessTokenNamesASigningKey()
        {
            ReadIssuedKeyId().Should().NotBeNullOrEmpty();
        }

        [Then("The published signing keys contain the key named by the access token")]
        public void ThenThePublishedSigningKeysContainTheKeyNamedByTheAccessToken()
        {
            _publishedKeys.Select(k => k.KeyId).Should().Contain(ReadIssuedKeyId());
        }

        [Then("Each published signing key has a key identifier")]
        public void ThenEachPublishedSigningKeyHasAKeyIdentifier()
        {
            _publishedKeys.Should().NotBeEmpty().And.OnlyContain(k => !string.IsNullOrEmpty(k.KeyId));
        }

        [Then("Each published signing key is marked for signature use")]
        public void ThenEachPublishedSigningKeyIsMarkedForSignatureUse()
        {
            _publishedKeys.Should().NotBeEmpty().And.OnlyContain(k => k.Use == "sig");
        }

        [Then("Each published signing key states its signing algorithm")]
        public void ThenEachPublishedSigningKeyStatesItsSigningAlgorithm()
        {
            _publishedKeys.Should().NotBeEmpty().And.OnlyContain(k => k.Algorithm == "RS256");
        }

        [Then("Both the current and the previous signing key are published")]
        public void ThenBothTheCurrentAndThePreviousSigningKeyArePublished()
        {
            _publishedKeys.Select(k => k.Modulus).Should().BeEquivalentTo([_currentKey!.Modulus, _previousKey!.Modulus]);
        }

        [Then("The access token signed with the previous signing key is accepted")]
        public async Task ThenTheAccessTokenSignedWithThePreviousSigningKeyIsAcceptedAsync()
        {
            await AssertHeldTokenIsAcceptedAsync();
        }

        [Then("The access token signed with the current signing key is accepted")]
        public async Task ThenTheAccessTokenSignedWithTheCurrentSigningKeyIsAcceptedAsync()
        {
            await AssertHeldTokenIsAcceptedAsync();
        }

        public void Dispose()
        {
            _rotationClient?.Dispose();
            _factory?.Dispose();
            _currentKey?.Dispose();
            _previousKey?.Dispose();
            _testAdminSigningKey?.Dispose();
            _testAdminResponse?.Dispose();
        }

        private HttpClient Client => _rotationClient ?? _defaultClient;

        private async Task<List<JwkResponse>> GetPublishedKeysAsync()
        {
            var jwks = await Client.GetFromJsonAsync<JwksResponse>(JwksPath);
            return jwks!.Keys.ToList();
        }

        private async Task<string> CreateTokenAsync(TestSigningKey key)
        {
            var keyId = (await GetPublishedKeysAsync()).Single(k => k.Modulus == key.Modulus).KeyId;

            var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Expires = DateTime.UtcNow.AddMinutes(30),
                Claims = new Dictionary<string, object>
                {
                    { "sub", "id" },
                    { "scope", "user" },
                    { "aud", Issuer }
                },
                SigningCredentials = key.CredentialsWithKeyId(keyId)
            };

            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
        }

        private async Task AssertHeldTokenIsAcceptedAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _heldToken);

            var response = await Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private string? ReadIssuedKeyId()
        {
            var cookie = _cookieContainer.GetAllCookies()[CookieConstants.TokenKey];
            cookie.Should().NotBeNull();

            var accessToken = JsonSerializer.Deserialize<AccessToken>(HttpUtility.UrlDecode(cookie!.Value))!;
            return new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token).Header.Kid;
        }
    }
}
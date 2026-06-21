using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RestSharp;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Infrastructure.Services.Github;
using TimeForCode.Authorization.Values;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    [TestClass]
    public class GithubServiceTests
    {
        private const string BaseUrl = "http://localhost:8081";
        private const string TestToken = "test-access-token";

        private MockHttpMessageHandler _mockHttp = default!;
        private GithubService _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockHttp = new MockHttpMessageHandler();

            var restClient = new RestClient(new RestClientOptions
            {
                ConfigureMessageHandler = _ => _mockHttp
            });

            var options = OptionsFactory.Create(new ExternalIdentityProviderOptions
            {
                CallbackUri = "http://localhost:8080/api/v1/authentication/callback",
                Github = new ExternalIdentityProvider
                {
                    LoginHost = "localhost",
                    LoginHostPort = 8081,
                    AccessTokenHost = "localhost",
                    AccessTokenHostPort = 8081,
                    RestApiHost = "localhost",
                    RestApiPort = 8081,
                    ClientId = "test-client",
                    ClientSecret = "test-secret",
                    IsHttps = false,
                    Issuer = BaseUrl,
                    MetaDataAddress = null
                }
            });

            _sut = new GithubService(options, restClient, NullLogger<GithubService>.Instance);
        }

        // ── GetAccessTokenAsync ──────────────────────────────────────────────────

        [TestMethod]
        public async Task GetAccessTokenAsync_WhenProviderReturnsToken_ReturnsSuccess()
        {
            var accessTokenResult = new AccessTokenPayload
            {
                TokenType = "bearer",
                AccessToken = TestToken,
                Scope = "user"
            };

            _mockHttp.When(HttpMethod.Post, $"{BaseUrl}/login/oauth/access_token")
                .Respond("application/json", JsonSerializer.Serialize(accessTokenResult));

            var result = await _sut.GetAccessTokenAsync("auth-code");

            result.IsSuccess.Should().BeTrue();
            result.Value.AccessToken.Should().Be(TestToken);
            result.Value.TokenType.Should().Be("bearer");
            result.Value.Scope.Should().Be("user");
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_WhenProviderReturnsError_ReturnsFailure()
        {
            _mockHttp.When(HttpMethod.Post, $"{BaseUrl}/login/oauth/access_token")
                .Respond(HttpStatusCode.BadRequest, "application/json", "{\"error\":\"bad_verification_code\"}");

            var result = await _sut.GetAccessTokenAsync("invalid-code");

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        // ── GetAccountInformation ────────────────────────────────────────────────

        [TestMethod]
        public async Task GetAccountInformation_WhenProviderReturnsUser_MapsAllFields()
        {
            var githubUser = new GithubUserPayload
            {
                Id = 42,
                Login = "johndoe",
                NodeId = "MDQ6VXNlcjE=",
                AvatarUrl = "https://avatars.githubusercontent.com/u/42",
                Name = "John Doe",
                Company = "ACME",
                Email = "john@example.com",
                Bio = "Developer",
                Location = "Netherlands"
            };

            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/user")
                .Respond("application/json", JsonSerializer.Serialize(githubUser));

            var result = await _sut.GetAccountInformation(new ExternalAccessToken { Token = TestToken });

            result.IsSuccess.Should().BeTrue();
            result.Value.Login.Should().Be("johndoe");
            result.Value.NodeId.Should().Be("MDQ6VXNlcjE=");
            result.Value.AvatarUrl.Should().Be("https://avatars.githubusercontent.com/u/42");
            result.Value.Name.Should().Be("John Doe");
            result.Value.Company.Should().Be("ACME");
            result.Value.Email.Should().Be("john@example.com");
            result.Value.Bio.Should().Be("Developer");
            result.Value.Location.Should().Be("Netherlands");
        }

        [TestMethod]
        public async Task GetAccountInformation_WhenTokenIsRevoked_ReturnsFailure()
        {
            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/user")
                .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"message\":\"Bad credentials\"}");

            var result = await _sut.GetAccountInformation(new ExternalAccessToken { Token = "revoked-token" });

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        // ── GetUserRepositoriesAsync ─────────────────────────────────────────────

        [TestMethod]
        public async Task GetUserRepositoriesAsync_WhenProviderReturnsRepos_ReturnsOnlyPublicOnes()
        {
            var repos = new[]
            {
                new GithubRepositoryPayload { Name = "public-repo", Description = "A public repo", StargazersCount = 5, Language = "C#", HtmlUrl = "https://github.com/johndoe/public-repo", IsPrivate = false },
                new GithubRepositoryPayload { Name = "private-repo", Description = "A private repo", StargazersCount = 0, Language = "C#", HtmlUrl = "https://github.com/johndoe/private-repo", IsPrivate = true }
            };

            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/user/repos")
                .Respond("application/json", JsonSerializer.Serialize(repos));

            var result = await _sut.GetUserRepositoriesAsync(TestToken);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value.Single().Name.Should().Be("public-repo");
        }

        [TestMethod]
        public async Task GetUserRepositoriesAsync_WhenAllReposArePrivate_ReturnsEmptyList()
        {
            var repos = new[]
            {
                new GithubRepositoryPayload { Name = "private-one", Description = null, StargazersCount = 0, Language = null, HtmlUrl = "https://github.com/johndoe/private-one", IsPrivate = true }
            };

            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/user/repos")
                .Respond("application/json", JsonSerializer.Serialize(repos));

            var result = await _sut.GetUserRepositoriesAsync(TestToken);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetUserRepositoriesAsync_MapsAllFieldsCorrectly()
        {
            var repos = new[]
            {
                new GithubRepositoryPayload { Name = "my-repo", Description = "Some description", StargazersCount = 99, Language = "TypeScript", HtmlUrl = "https://github.com/johndoe/my-repo", IsPrivate = false }
            };

            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/user/repos")
                .Respond("application/json", JsonSerializer.Serialize(repos));

            var result = await _sut.GetUserRepositoriesAsync(TestToken);

            result.IsSuccess.Should().BeTrue();
            var repo = result.Value.Single();
            repo.Name.Should().Be("my-repo");
            repo.Description.Should().Be("Some description");
            repo.StarCount.Should().Be(99);
            repo.Language.Should().Be("TypeScript");
            repo.Url.Should().Be("https://github.com/johndoe/my-repo");
        }

        [TestMethod]
        public async Task GetUserRepositoriesAsync_WhenTokenIsRevoked_ReturnsFailure()
        {
            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/user/repos")
                .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"message\":\"Bad credentials\"}");

            var result = await _sut.GetUserRepositoriesAsync("revoked-token");

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        // ── GetRepositoryAsync ───────────────────────────────────────────────────

        [TestMethod]
        public async Task GetRepositoryAsync_WhenRepoExists_ReturnsMappedRepository()
        {
            var repo = new GithubRepositoryPayload { Name = "my-repo", Description = "A test repository", StargazersCount = 12, Language = "C#", HtmlUrl = "https://github.com/johndoe/my-repo", IsPrivate = false };

            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/repos/johndoe/my-repo")
                .Respond("application/json", JsonSerializer.Serialize(repo));

            var result = await _sut.GetRepositoryAsync("johndoe", "my-repo", TestToken);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be("my-repo");
            result.Value.Description.Should().Be("A test repository");
            result.Value.StarCount.Should().Be(12);
            result.Value.Language.Should().Be("C#");
            result.Value.Url.Should().Be("https://github.com/johndoe/my-repo");
        }

        [TestMethod]
        public async Task GetRepositoryAsync_WhenRepoNotFound_ReturnsFailure()
        {
            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/repos/johndoe/nonexistent")
                .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"Not Found\"}");

            var result = await _sut.GetRepositoryAsync("johndoe", "nonexistent", TestToken);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        // ── GetTokenValidationParameters ─────────────────────────────────────────

        [TestMethod]
        public async Task GetTokenValidationParameters_ReturnsParametersWithCorrectIssuerAndAudience()
        {
            var parameters = await _sut.GetTokenValidationParameters();

            parameters.ValidateIssuer.Should().BeTrue();
            parameters.ValidIssuer.Should().Be(BaseUrl);
            parameters.ValidateAudience.Should().BeTrue();
            parameters.ValidAudiences.Should().Contain("test-client");
            parameters.ValidateLifetime.Should().BeTrue();
            parameters.ValidateIssuerSigningKey.Should().BeTrue();
        }
    }
}
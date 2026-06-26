using FluentAssertions;
using Microsoft.Extensions.Options;
using RestSharp;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimeForCode.Donation.Infrastructure.Options;
using TimeForCode.Donation.Infrastructure.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace TimeForCode.Donation.Infrastructure.Tests.Services
{
    [TestClass]
    public class GithubRepositoryApiServiceTests
    {
        private const string BaseUrl = "http://localhost:8081";

        private MockHttpMessageHandler _mockHttp = default!;
        private GithubRepositoryApiService _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockHttp = new MockHttpMessageHandler();

            var restClient = new RestClient(new RestClientOptions
            {
                ConfigureMessageHandler = _ => _mockHttp
            });

            var options = OptionsFactory.Create(new GithubApiOptions
            {
                BaseUrl = BaseUrl
            });

            _sut = new GithubRepositoryApiService(restClient, options);
        }

        // ── GetRepositoryMetadataAsync ───────────────────────────────────────────

        [TestMethod]
        public async Task GetRepositoryMetadataAsync_ValidPublicRepository_ReturnsSuccess()
        {
            var response = BuildGithubRepositoryResponse();

            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/repos/owner/repo")
                .Respond("application/json", JsonSerializer.Serialize(response));

            var result = await _sut.GetRepositoryMetadataAsync(new Uri("https://github.com/owner/repo"));

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetRepositoryMetadataAsync_ValidRepository_MapsAllFieldsCorrectly()
        {
            var response = BuildGithubRepositoryResponse();

            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/repos/owner/repo")
                .Respond("application/json", JsonSerializer.Serialize(response));

            var result = await _sut.GetRepositoryMetadataAsync(new Uri("https://github.com/owner/repo"));

            result.IsSuccess.Should().BeTrue();
            var snapshot = result.Value;
            snapshot.Name.Should().Be(response.Name);
            snapshot.FullName.Should().Be(response.FullName);
            snapshot.Description.Should().Be(response.Description);
            snapshot.HtmlUrl.Should().Be(response.HtmlUrl);
            snapshot.Language.Should().Be(response.Language);
            snapshot.Topics.Should().BeEquivalentTo(response.Topics);
            snapshot.StargazersCount.Should().Be(response.StargazersCount);
            snapshot.ForksCount.Should().Be(response.ForksCount);
            snapshot.OpenIssuesCount.Should().Be(response.OpenIssuesCount);
            snapshot.Homepage.Should().Be(response.Homepage);
            snapshot.DefaultBranch.Should().Be(response.DefaultBranch);
            snapshot.OwnerLogin.Should().Be(response.Owner.Login);
            snapshot.OwnerAvatarUrl.Should().Be(response.Owner.AvatarUrl);
            snapshot.CreatedAt.Should().Be(response.CreatedAt);
            snapshot.UpdatedAt.Should().Be(response.UpdatedAt);
            snapshot.PushedAt.Should().Be(response.PushedAt);
            snapshot.IsPrivate.Should().BeFalse();
            snapshot.IsArchived.Should().BeFalse();
        }

        [TestMethod]
        public async Task GetRepositoryMetadataAsync_NonGithubUrl_ReturnsFailure()
        {
            var result = await _sut.GetRepositoryMetadataAsync(new Uri("https://gitlab.com/owner/repo"));

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task GetRepositoryMetadataAsync_UrlWithInvalidSegments_ReturnsFailure()
        {
            var result = await _sut.GetRepositoryMetadataAsync(new Uri("https://github.com/owner"));

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task GetRepositoryMetadataAsync_UrlWithTooManySegments_ReturnsFailure()
        {
            var result = await _sut.GetRepositoryMetadataAsync(new Uri("https://github.com/owner/repo/extra"));

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task GetRepositoryMetadataAsync_ApiCallFails_ReturnsFailure()
        {
            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/repos/owner/repo")
                .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"Not Found\"}");

            var result = await _sut.GetRepositoryMetadataAsync(new Uri("https://github.com/owner/repo"));

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task GetRepositoryMetadataAsync_ApiReturnsServerError_ReturnsFailure()
        {
            _mockHttp.When(HttpMethod.Get, $"{BaseUrl}/repos/owner/repo")
                .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"Internal Server Error\"}");

            var result = await _sut.GetRepositoryMetadataAsync(new Uri("https://github.com/owner/repo"));

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        private static GithubRepositoryResponseTestModel BuildGithubRepositoryResponse()
        {
            return new GithubRepositoryResponseTestModel
            {
                Name = "test-repo",
                FullName = "owner/test-repo",
                Description = "A test repository",
                HtmlUrl = "https://github.com/owner/test-repo",
                Language = "C#",
                Topics = ["dotnet", "testing"],
                StargazersCount = 42,
                ForksCount = 5,
                OpenIssuesCount = 3,
                Homepage = "https://example.com",
                DefaultBranch = "main",
                License = new GithubLicenseResponseTestModel { SpdxId = "MIT" },
                Owner = new GithubOwnerResponseTestModel
                {
                    Login = "owner",
                    AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4"
                },
                CreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PushedAt = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero),
                IsPrivate = false,
                Archived = false
            };
        }

        // Local DTO models that mirror GithubRepositoryResponse so we can serialize test payloads.
        private sealed class GithubRepositoryResponseTestModel
        {
            public required string Name { get; init; }

            [JsonPropertyName("full_name")]
            public required string FullName { get; init; }

            public string? Description { get; init; }

            [JsonPropertyName("html_url")]
            public required string HtmlUrl { get; init; }

            public string? Language { get; init; }

            public IReadOnlyList<string> Topics { get; init; } = [];

            [JsonPropertyName("stargazers_count")]
            public int StargazersCount { get; init; }

            [JsonPropertyName("forks_count")]
            public int ForksCount { get; init; }

            [JsonPropertyName("open_issues_count")]
            public int OpenIssuesCount { get; init; }

            public string? Homepage { get; init; }

            [JsonPropertyName("default_branch")]
            public required string DefaultBranch { get; init; }

            public GithubLicenseResponseTestModel? License { get; init; }

            public required GithubOwnerResponseTestModel Owner { get; init; }

            [JsonPropertyName("created_at")]
            public required DateTimeOffset CreatedAt { get; init; }

            [JsonPropertyName("updated_at")]
            public required DateTimeOffset UpdatedAt { get; init; }

            [JsonPropertyName("pushed_at")]
            public required DateTimeOffset PushedAt { get; init; }

            [JsonPropertyName("private")]
            public bool IsPrivate { get; init; }

            public bool Archived { get; init; }
        }

        private sealed class GithubOwnerResponseTestModel
        {
            public required string Login { get; init; }

            [JsonPropertyName("avatar_url")]
            public required string AvatarUrl { get; init; }
        }

        private sealed class GithubLicenseResponseTestModel
        {
            [JsonPropertyName("spdx_id")]
            public string? SpdxId { get; init; }
        }
    }
}
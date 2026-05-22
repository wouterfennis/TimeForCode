using System.Text.Json.Serialization;

namespace TimeForCode.Donation.Infrastructure.Services
{
    internal class GithubRepositoryResponse
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

        public GithubLicenseResponse? License { get; init; }

        public required GithubOwnerResponse Owner { get; init; }

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

    internal class GithubOwnerResponse
    {
        public required string Login { get; init; }

        [JsonPropertyName("avatar_url")]
        public required string AvatarUrl { get; init; }
    }

    internal class GithubLicenseResponse
    {
        [JsonPropertyName("spdx_id")]
        public string? SpdxId { get; init; }
    }
}
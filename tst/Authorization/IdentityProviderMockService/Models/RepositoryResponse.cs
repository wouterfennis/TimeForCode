using System.Text.Json.Serialization;

namespace IdentityProviderMockService.Models
{
    public class RepositoryResponse
    {
        public required string Name { get; init; }

        [JsonPropertyName("full_name")]
        public required string FullName { get; init; }

        public string? Description { get; init; }

        [JsonPropertyName("html_url")]
        public required string HtmlUrl { get; init; }

        public string? Language { get; init; }

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; init; }

        [JsonPropertyName("forks_count")]
        public int ForksCount { get; init; }

        [JsonPropertyName("open_issues_count")]
        public int OpenIssuesCount { get; init; }

        [JsonPropertyName("default_branch")]
        public required string DefaultBranch { get; init; }

        public required OwnerResponse Owner { get; init; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; init; }

        [JsonPropertyName("pushed_at")]
        public DateTimeOffset PushedAt { get; init; }

        [JsonPropertyName("private")]
        public bool IsPrivate { get; init; }

        public bool Archived { get; init; }
    }

    public class OwnerResponse
    {
        public required string Login { get; init; }

        [JsonPropertyName("avatar_url")]
        public required string AvatarUrl { get; init; }
    }
}
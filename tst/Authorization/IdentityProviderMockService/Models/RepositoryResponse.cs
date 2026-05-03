using System.Text.Json.Serialization;

namespace IdentityProviderMockService.Models
{
    public class RepositoryResponse
    {
        public required string Name { get; init; }
        public string? Description { get; init; }

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; init; }

        public string? Language { get; init; }

        [JsonPropertyName("html_url")]
        public required string HtmlUrl { get; init; }

        [JsonPropertyName("private")]
        public bool IsPrivate { get; init; }
    }
}

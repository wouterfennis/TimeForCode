using System.Text.Json.Serialization;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    internal sealed class AccessTokenPayload
    {
        [JsonPropertyName("token_type")]
        public required string TokenType { get; init; }

        [JsonPropertyName("access_token")]
        public required string AccessToken { get; init; }

        [JsonPropertyName("scope")]
        public required string Scope { get; init; }
    }

    internal sealed class GithubUserPayload
    {
        [JsonPropertyName("id")]
        public required uint Id { get; init; }

        [JsonPropertyName("login")]
        public required string Login { get; init; }

        [JsonPropertyName("node_id")]
        public required string NodeId { get; init; }

        [JsonPropertyName("avatar_url")]
        public required string AvatarUrl { get; init; }

        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("company")]
        public required string? Company { get; init; }

        [JsonPropertyName("email")]
        public required string Email { get; init; }

        [JsonPropertyName("bio")]
        public string? Bio { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }
    }

    internal sealed class GithubRepositoryPayload
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; init; }

        [JsonPropertyName("language")]
        public string? Language { get; init; }

        [JsonPropertyName("html_url")]
        public required string HtmlUrl { get; init; }

        [JsonPropertyName("private")]
        public bool IsPrivate { get; init; }
    }
}
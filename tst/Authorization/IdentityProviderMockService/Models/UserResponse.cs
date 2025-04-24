using System.Text.Json.Serialization;

namespace IdentityProviderMockService.Models
{
    public class UserResponse
    {
        public required uint Id { get; init; }
        public required string Login { get; init; }
        [JsonPropertyName("node_id")]
        public required string NodeId { get; init; }
        [JsonPropertyName("avatar_url")]
        public required string AvatarUrl { get; init; }
        public required string Name { get; init; }
        public required string? Company { get; init; }
        public required string Email { get; init; }
    }
}
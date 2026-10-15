using MongoDB.Bson.Serialization.Attributes;

namespace TimeForCode.Authorization.Domain.Entities
{
    // Documents written before the GitHub token was dropped may still carry the removed field; it is ignored when reading.
    [BsonIgnoreExtraElements]
    public class AccountInformation : DocumentEntity
    {
        public required string IdentityProviderId { get; init; }
        public required string Login { get; init; }
        public required string NodeId { get; init; }
        public required string AvatarUrl { get; init; }
        public required string Name { get; init; }
        public required string? Company { get; init; }
        public required string Email { get; init; }
        public required string? Bio { get; init; }
        public required string? Location { get; init; }
    }
}
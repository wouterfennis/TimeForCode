using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TimeForCode.Authorization.Domain
{
    public class AccountInformation
    {
        [BsonId]
        public required ObjectId Id { get; init; }
        public required string IdentityProviderId { get; init; }
        public required string Login { get; init; }
        public required string NodeId { get; init; }
        public required string AvatarUrl { get; init; }
        public required string Name { get; init; }
        public required string? Company { get; init; }
        public required string Email { get; init; }
    }
}
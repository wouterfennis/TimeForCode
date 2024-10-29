using MongoDB.Bson;

namespace TimeForCode.Authorization.Domain.Entities
{
    public class RefreshToken : DocumentEntity
    {
        public required string Token { get; init; }
        public required DateTimeOffset ExpiresAfter { get; init; }

        public bool IsExpired(DateTimeOffset referenceDateTime) => referenceDateTime > ExpiresAfter;
    }
}

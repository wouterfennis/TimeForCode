using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TimeForCode.Authorization.Domain.Entities
{
    public class RefreshToken : DocumentEntity
    {
        /// <summary>
        /// SHA-256 hex digest of the token handed to the client; the readable value is never persisted.
        /// </summary>
        public required string TokenHash { get; init; }
        public required string UserId { get; init; }

        /// <summary>
        /// Stored as a BSON date so the TTL index can remove the record once it has passed.
        /// </summary>
        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset ExpiresAfter { get; init; }

        /// <summary>
        /// Set when the token was exchanged for a new one; presenting it again signals token theft.
        /// </summary>
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset? RotatedAt { get; private set; }

        public static RefreshToken Issue(string tokenHash, string userId, DateTimeOffset now, int lifetimeInDays)
        {
            return new RefreshToken
            {
                Id = ObjectId.GenerateNewId(),
                TokenHash = tokenHash,
                UserId = userId,
                ExpiresAfter = now.AddDays(lifetimeInDays)
            };
        }

        public RefreshTokenReplacementCheck CheckReplaceable(DateTimeOffset referenceDateTime)
        {
            if (IsExpired(referenceDateTime))
            {
                return RefreshTokenReplacementCheck.Expired;
            }

            return IsRotated ? RefreshTokenReplacementCheck.Replayed : RefreshTokenReplacementCheck.Allowed;
        }

        public void MarkRotated(DateTimeOffset rotatedAt)
        {
            RotatedAt = rotatedAt;
        }

        public bool IsExpired(DateTimeOffset referenceDateTime) => referenceDateTime > ExpiresAfter;
        public bool IsRotated => RotatedAt.HasValue;
    }
}
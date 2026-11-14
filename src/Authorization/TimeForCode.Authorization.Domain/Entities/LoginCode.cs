using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TimeForCode.Authorization.Domain.Entities
{
    /// <summary>
    /// A single-use code issued after a successful sign-in at the identity provider. The client exchanges it for tokens
    /// together with the PKCE verifier whose challenge was sent at login, so a code obtained in another browser is useless.
    /// Only the hash of the code is stored.
    /// </summary>
    public class LoginCode : DocumentEntity
    {
        public required string CodeHash { get; init; }

        public required string UserId { get; init; }

        public required bool IsNewUser { get; init; }

        /// <summary>
        /// The S256 PKCE challenge the client sent at login.
        /// </summary>
        public required string CodeChallenge { get; init; }

        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset ExpiresAfter { get; init; }

        public static LoginCode Issue(string codeHash, string userId, bool isNewUser, string codeChallenge, DateTimeOffset expiresAfter)
        {
            return new LoginCode
            {
                Id = ObjectId.GenerateNewId(),
                CodeHash = codeHash,
                UserId = userId,
                IsNewUser = isNewUser,
                CodeChallenge = codeChallenge,
                ExpiresAfter = expiresAfter
            };
        }

        public bool IsExpired(DateTimeOffset referenceDateTime) => referenceDateTime > ExpiresAfter;
    }
}
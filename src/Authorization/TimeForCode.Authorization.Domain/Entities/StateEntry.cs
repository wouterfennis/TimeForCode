using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Domain.Entities
{
    /// <summary>
    /// The start of one login attempt, kept until the callback consumes it or it expires.
    /// </summary>
    public class StateEntry : DocumentEntity
    {
        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

        public required string Key { get; init; }
        public required IdentityProvider IdentityProvider { get; init; }
        public required Uri RedirectUri { get; init; }

        /// <summary>
        /// PKCE code verifier; only its SHA-256 challenge travels through the browser.
        /// </summary>
        public required string CodeVerifier { get; init; }

        /// <summary>
        /// Stored as a BSON date so the TTL index can remove the record once it has passed.
        /// </summary>
        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset ExpiresAfter { get; init; }

        public static StateEntry Start(string key, IdentityProvider identityProvider, Uri redirectUri, string codeVerifier, DateTimeOffset now)
        {
            return new StateEntry
            {
                Id = ObjectId.GenerateNewId(),
                Key = key,
                IdentityProvider = identityProvider,
                RedirectUri = redirectUri,
                CodeVerifier = codeVerifier,
                ExpiresAfter = now.Add(Lifetime)
            };
        }

        public bool IsExpired(DateTimeOffset referenceDateTime) => referenceDateTime > ExpiresAfter;
    }
}
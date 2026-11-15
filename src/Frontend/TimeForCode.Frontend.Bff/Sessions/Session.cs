using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// The server-side session behind the opaque cookie: the tokens never leave the BFF.
    /// Only the hash of the session id is stored, so a database read does not give a usable cookie.
    /// </summary>
    public sealed class Session
    {
        [BsonId]
        public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

        public required string IdHash { get; init; }

        public required string AccessToken { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset AccessTokenExpiresAt { get; set; }

        public required string RefreshToken { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset RefreshTokenExpiresAt { get; set; }

        public required bool IsNewUser { get; init; }

        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset CreatedAt { get; init; }

        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset LastSeenAt { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset AbsoluteExpiresAt { get; init; }

        /// <summary>
        /// When the database removes the session: the earlier of the idle and the absolute limit.
        /// </summary>
        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        /// While set and in the future, one request holds the right to use the refresh token. A refresh token may be used once;
        /// a second use is read as theft, so concurrent requests must not refresh with the same token.
        /// </summary>
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset? RefreshLeaseUntil { get; set; }

        /// <summary>
        /// Who holds the lease. Saving the rotated tokens and releasing the lease only work for the holder, so a slow request whose
        /// lease expired cannot overwrite the tokens of the request that took over.
        /// </summary>
        public string? RefreshLeaseId { get; set; }
    }

    /// <summary>
    /// A sign-in that was started and not yet completed: the PKCE verifier waits here for the code.
    /// </summary>
    public sealed class LoginAttempt
    {
        [BsonId]
        public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

        public required string IdHash { get; init; }

        public required string CodeVerifier { get; init; }

        public required string ReturnUrl { get; init; }

        [BsonRepresentation(BsonType.DateTime)]
        public required DateTimeOffset ExpiresAt { get; init; }
    }

    /// <summary>
    /// The tokens the Authorization API issued, in the BFF own terms.
    /// </summary>
    /// <summary>
    /// The start of an admin passkey ceremony: the options for the browser and the id that completes it. The ceremony state stays at the Authorization API.
    /// </summary>
    public sealed record AdminCeremony(string CeremonyId, string OptionsJson);

    public sealed record IssuedTokens(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        bool IsNewUser);
}
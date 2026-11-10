namespace TimeForCode.Authorization.Values
{
    /// <summary>
    /// A long-lived refresh token that is exchanged for a new access token and a new refresh token.
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// The opaque refresh token value; single use, it is replaced by a new one on every refresh.
        /// </summary>
        public required string Token { get; init; }

        /// <summary>
        /// The moment the refresh token expires, as an ISO 8601 date and time with offset; the token is invalid after this instant.
        /// </summary>
        public required DateTimeOffset ExpiresAfter { get; init; }

        /// <summary>
        /// Tells whether the token has expired at the given moment.
        /// </summary>
        /// <param name="referenceDateTime">The moment to compare with <see cref="ExpiresAfter"/>.</param>
        /// <returns><c>true</c> when the moment is after the expiry.</returns>
        public bool IsExpired(DateTimeOffset referenceDateTime) => referenceDateTime > ExpiresAfter;
    }
}
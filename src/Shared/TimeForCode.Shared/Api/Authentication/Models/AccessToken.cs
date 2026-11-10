namespace TimeForCode.Shared.Api.Authentication.Models
{
    /// <summary>
    /// A short-lived access token issued by the Authorization API.
    /// </summary>
    public class AccessToken
    {
        /// <summary>
        /// The signed JSON Web Token (JWT, RS256); send it as a bearer token. Verify it with the keys of the JWKS endpoint.
        /// </summary>
        public required string Token { get; init; }

        /// <summary>
        /// The moment the token expires, as an ISO 8601 date and time with offset; the token is invalid after this instant.
        /// </summary>
        public required DateTimeOffset ExpiresAfter { get; init; }
    }
}
namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// The token endpoints of the Authorization API, called server to server with the refresh token or the one-time code as credential.
    /// </summary>
    public interface IAuthorizationApi
    {
        /// <summary>
        /// Exchanges the single-use code and the PKCE verifier for tokens; null when the API rejects them.
        /// </summary>
        Task<IssuedTokens?> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken);

        /// <summary>
        /// Rotates the refresh token; null when the API rejects it (unknown, expired or already used).
        /// </summary>
        Task<IssuedTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
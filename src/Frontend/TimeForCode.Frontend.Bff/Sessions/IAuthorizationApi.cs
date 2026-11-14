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

        /// <summary>
        /// Starts the admin passkey registration; null when the bootstrap secret is wrong or the admin role is already claimed.
        /// </summary>
        Task<AdminCeremony?> AdminRegistrationOptionsAsync(string bootstrapSecret, CancellationToken cancellationToken);

        /// <summary>
        /// Completes the admin passkey registration and returns the admin tokens; null when it is rejected.
        /// </summary>
        Task<IssuedTokens?> AdminCompleteRegistrationAsync(string bootstrapSecret, string credentialJson, string ceremonyId, CancellationToken cancellationToken);

        /// <summary>
        /// Starts the admin passkey sign-in; null when no admin passkey is registered yet.
        /// </summary>
        Task<AdminCeremony?> AdminAuthenticationOptionsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Completes the admin passkey sign-in and returns the admin tokens; null when it is rejected.
        /// </summary>
        Task<IssuedTokens?> AdminCompleteAuthenticationAsync(string credentialJson, string ceremonyId, CancellationToken cancellationToken);
    }
}
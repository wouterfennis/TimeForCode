using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// Calls the Authorization API code exchange, refresh and logout. A rejected credential (400) is a normal outcome;
    /// anything else (the API is down, a server error) is an exception, so a hiccup does not end a session.
    /// </summary>
    public sealed class AuthorizationApi(HttpClient client) : IAuthorizationApi
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public async Task<IssuedTokens?> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
        {
            using var response = await client.PostAsJsonAsync("api/v1/authentication/token", new { code, codeVerifier }, Json, cancellationToken);
            return await ReadTokensAsync(response, cancellationToken);
        }

        public async Task<IssuedTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            using var response = await client.PostAsJsonAsync("api/v1/authentication/refresh", RefreshBody(refreshToken), Json, cancellationToken);
            return await ReadTokensAsync(response, cancellationToken);
        }

        public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            using var response = await client.PostAsJsonAsync("api/v1/authentication/logout", RefreshBody(refreshToken), Json, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        // The API reads the token value; the expiry in the body is informational, so a far date keeps the request well formed.
        private static object RefreshBody(string refreshToken) => new { refreshToken = new { token = refreshToken, expiresAfter = DateTimeOffset.MaxValue } };

        private static async Task<IssuedTokens?> ReadTokensAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, cancellationToken)
                ?? throw new InvalidOperationException("The Authorization API answered without tokens.");
            return new IssuedTokens(
                body.AccessToken.Token,
                body.AccessToken.ExpiresAfter,
                body.RefreshToken.Token,
                body.RefreshToken.ExpiresAfter,
                body.IsNewUser);
        }

        private sealed record TokenResponse(TokenInfo AccessToken, TokenInfo RefreshToken, bool IsNewUser);

        private sealed record TokenInfo(string Token, DateTimeOffset ExpiresAfter);
    }
}
using System.Text.Json;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Shared.Api.Authentication
{
    /// <summary>
    /// Reads the JWT out of the value of the access-token cookie (<see cref="CookieConstants.TokenKey"/>).
    /// </summary>
    public static class AccessTokenCookie
    {
        /// <summary>
        /// Returns the JWT in the cookie value, or <c>null</c> when the value is missing, not valid JSON or holds no token.
        /// </summary>
        /// <param name="cookieValue">The raw cookie value.</param>
        /// <returns>The JWT, or <c>null</c>.</returns>
        public static string? TryReadToken(string? cookieValue)
        {
            if (string.IsNullOrEmpty(cookieValue))
            {
                return null;
            }

            try
            {
                string? token = JsonSerializer.Deserialize<AccessToken>(cookieValue)?.Token;
                return string.IsNullOrEmpty(token) ? null : token;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
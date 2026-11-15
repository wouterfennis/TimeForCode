using System.Text.Json;
using TimeForCode.Authorization.Commands;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// The token cookies; shared so every endpoint that sets or expires them uses identical attributes.
    /// The access token cookie goes to every route (whatever fronts the API can turn it into the bearer header). The refresh token
    /// cookie is limited to the session path, so the browser sends it to refresh and logout and nowhere else.
    /// </summary>
    internal static class TokenCookies
    {
        internal static CookieOptions CreateOptions(DateTimeOffset? expires = null, TimeSpan? maxAge = null, string path = "/")
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = path,
                Expires = expires,
                MaxAge = maxAge
            };
        }

        internal static void Append(HttpResponse response, TokenResult result, string refreshPath)
        {
            response.Cookies.Append(CookieConstants.TokenKey, JsonSerializer.Serialize(result.InternalAccessToken), CreateOptions(result.InternalAccessToken.ExpiresAfter));
            response.Cookies.Append(CookieConstants.RefreshTokenKey, JsonSerializer.Serialize(result.RefreshToken), CreateOptions(result.RefreshToken.ExpiresAfter, path: refreshPath));
        }

        internal static void Expire(HttpResponse response, string refreshPath)
        {
            // Delete only matches the cookie when path, secure and samesite equal those used when it was set.
            response.Cookies.Delete(CookieConstants.TokenKey, CreateOptions());
            response.Cookies.Delete(CookieConstants.RefreshTokenKey, CreateOptions(path: refreshPath));
        }
    }
}
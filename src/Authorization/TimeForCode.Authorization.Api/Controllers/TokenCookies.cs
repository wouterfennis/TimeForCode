using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// The attributes of the token cookies; shared so every endpoint that sets or expires them uses identical ones.
    /// </summary>
    internal static class TokenCookies
    {
        internal static CookieOptions CreateOptions(DateTimeOffset? expires = null, TimeSpan? maxAge = null)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expires,
                MaxAge = maxAge
            };
        }

        internal static void Expire(HttpResponse response)
        {
            // Delete only matches the cookie when path, secure and samesite equal those used when it was set.
            var cookieOptions = CreateOptions();

            response.Cookies.Delete(CookieConstants.TokenKey, cookieOptions);
            response.Cookies.Delete(CookieConstants.RefreshTokenKey, cookieOptions);
        }
    }
}
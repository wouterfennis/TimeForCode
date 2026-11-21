namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// The two cookies of the BFF and how they are read and written. The session cookie is the only credential
    /// the browser holds: an opaque random id with the <c>__Host-</c> prefix, so it is <c>Secure</c>, has <c>Path=/</c> and no <c>Domain</c>.
    /// </summary>
    internal static class BrowserSession
    {
        public const string SessionCookie = "__Host-session";
        public const string LoginCookie = "__Host-login";

        private static CookieOptions Options(SameSiteMode sameSite, TimeSpan? maxAge = null) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = sameSite,
            Path = "/",
            MaxAge = maxAge
        };

        /// <summary>
        /// The session behind the request cookie; a cookie that no longer maps to a session is cleared.
        /// </summary>
        public static async Task<Session?> ResolveAsync(HttpContext context, SessionService sessions)
        {
            context.Request.Cookies.TryGetValue(SessionCookie, out var sessionId);
            var session = await sessions.FindSessionAsync(sessionId, context.RequestAborted);
            if (session is null && sessionId is not null)
            {
                ClearSessionCookieWhenTheResponseStarts(context);
            }

            return session;
        }

        /// <summary>
        /// Strict: no cross-site request carries it. It is set during a cross-site redirect chain and sent from the next same-site request on.
        /// </summary>
        public static void SetSessionCookie(HttpContext context, string sessionId) =>
            context.Response.Cookies.Append(SessionCookie, sessionId, Options(SameSiteMode.Strict));

        /// <summary>
        /// Added as the response starts, so the proxy header transforms (which remove Set-Cookie of the API) cannot strip it.
        /// </summary>
        public static void ClearSessionCookieWhenTheResponseStarts(HttpContext context) =>
            context.Response.OnStarting(() =>
            {
                ClearSessionCookie(context);
                return Task.CompletedTask;
            });

        public static void ClearSessionCookie(HttpContext context) =>
            context.Response.Cookies.Delete(SessionCookie, Options(SameSiteMode.Strict));

        /// <summary>
        /// Lax: the browser comes back from another site (the Authorization API) with a top-level GET and must still send it.
        /// </summary>
        public static void SetLoginCookie(HttpContext context, string loginId, TimeSpan lifetime) =>
            context.Response.Cookies.Append(LoginCookie, loginId, Options(SameSiteMode.Lax, lifetime));

        public static void ClearLoginCookie(HttpContext context) =>
            context.Response.Cookies.Delete(LoginCookie, Options(SameSiteMode.Lax));
    }
}
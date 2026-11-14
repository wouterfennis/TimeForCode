namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Of the Authorization API only the user endpoints are reachable through the BFF. The token endpoints (code exchange,
    /// refresh, logout) are for the BFF itself, and the sign-in runs through <c>/bff/login</c>, so a script cannot call them
    /// with a stolen code or token through the session cookie.
    /// </summary>
    internal static class AuthRouteAllowlist
    {
        private const string Prefix = "/api/auth/";
        private const string UserPath = "/api/auth/v1/user";

        public static IApplicationBuilder UseAuthRouteAllowlist(this IApplicationBuilder app) => app.Use((context, next) =>
        {
            var path = context.Request.Path;
            var isAuthRoute = path.StartsWithSegments(Prefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
            if (isAuthRoute && !path.StartsWithSegments(UserPath, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return next(context);
        });
    }
}
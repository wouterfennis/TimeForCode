namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Second CSRF layer after <c>SameSite=Strict</c>. A state-changing request must carry <c>X-Requested-With</c>
    /// (a cross-site form cannot set it, and a cross-origin script that tries gets no CORS preflight approval) and,
    /// when the browser names an <c>Origin</c>, it must be this host: a script on another origin is refused even if it found a way to set the header.
    /// </summary>
    internal static class CsrfHeaderCheck
    {
        private const string HeaderName = "X-Requested-With";

        public static IApplicationBuilder UseCsrfHeaderCheck(this IApplicationBuilder app) => app.Use((context, next) =>
        {
            var method = context.Request.Method;
            var isSafe = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);
            if (isSafe || (context.Request.Headers.ContainsKey(HeaderName) && IsSameOrigin(context.Request)))
            {
                return next(context);
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        });

        // A browser sends Origin on every cross-origin request and on same-origin POSTs; a request without it is not cross-origin script.
        private static bool IsSameOrigin(HttpRequest request)
        {
            var origin = request.Headers.Origin.ToString();
            return origin.Length == 0
                || string.Equals(origin, $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase);
        }
    }
}
namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Second CSRF layer after <c>SameSite=Strict</c>. A state-changing request must carry <c>X-Requested-With</c>
    /// (a cross-site form cannot set it, and a cross-origin script that tries gets no CORS preflight approval) and,
    /// it must name this host as its <c>Origin</c>: a script on another origin is refused even if it found a way to set the header, and so is a request
    /// that names none, because a browser sends <c>Origin</c> on every same-origin POST, PUT, PATCH and DELETE.
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

        private static bool IsSameOrigin(HttpRequest request) =>
            string.Equals(request.Headers.Origin.ToString(), $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase);
    }
}
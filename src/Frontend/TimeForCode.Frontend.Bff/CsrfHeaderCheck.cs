namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Second CSRF layer after <c>SameSite=Strict</c>: a state-changing request must carry <c>X-Requested-With</c>.
    /// A cross-site form cannot set it, and a cross-origin script that tries gets no CORS preflight approval.
    /// </summary>
    internal static class CsrfHeaderCheck
    {
        private const string HeaderName = "X-Requested-With";

        public static IApplicationBuilder UseCsrfHeaderCheck(this IApplicationBuilder app) => app.Use((context, next) =>
        {
            var method = context.Request.Method;
            var isSafe = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);
            if (isSafe || context.Request.Headers.ContainsKey(HeaderName))
            {
                return next(context);
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        });
    }
}
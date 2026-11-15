namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Security headers for the browser on every response, errors and forwarded API responses included.
    /// Separate from the APIs' <c>TimeForCode.Shared.Api.Extensions.SecurityHeadersExtensions</c>: a page needs a CSP, whose <c>frame-ancestors</c> replaces <c>X-Frame-Options</c>.
    /// </summary>
    internal static class BrowserSecurityHeaders
    {
        private const string ContentSecurityPolicy =
            "default-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

        public static IApplicationBuilder UseBrowserSecurityHeaders(this IApplicationBuilder app) => app.Use((context, next) =>
        {
            // Set just before the response starts, so they also replace headers copied from a forwarded API response.
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
                headers.XContentTypeOptions = "nosniff";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });
            return next(context);
        });
    }
}
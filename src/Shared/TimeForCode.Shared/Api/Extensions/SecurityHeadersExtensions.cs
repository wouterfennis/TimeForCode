using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TimeForCode.Shared.Api.Extensions
{
    /// <summary>
    /// Extension methods for adding security headers to the HTTP pipeline.
    /// </summary>
    public static class SecurityHeadersExtensions
    {
        /// <summary>
        /// Adds security headers (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection) to every response.
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                await next();
            });
        }
    }
}
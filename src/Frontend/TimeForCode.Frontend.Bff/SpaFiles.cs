using Microsoft.Net.Http.Headers;

namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Serves the SPA build output: hashed assets cached for a year, everything else (index.html) revalidated.
    /// Client-side routes fall back to index.html; a missing file (a path with an extension) stays 404.
    /// </summary>
    internal static class SpaFiles
    {
        private static readonly StaticFileOptions Options = new()
        {
            OnPrepareResponse = context => context.Context.Response.Headers.CacheControl =
                context.Context.Request.Path.StartsWithSegments("/assets")
                    ? "public, max-age=31536000, immutable"
                    : CacheControlHeaderValue.NoCacheString,
        };

        public static void MapSpaFiles(this WebApplication app)
        {
            app.UseStaticFiles(Options);
            // An API path without a proxy route is a 404, not the SPA's index.html.
            app.MapGroup("/api").MapFallback(() => Results.NotFound());
            app.MapFallbackToFile("index.html", Options);
        }
    }
}
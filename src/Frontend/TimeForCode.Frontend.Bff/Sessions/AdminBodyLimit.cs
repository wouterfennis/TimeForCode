using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// The admin passkey calls are open to anyone, so their bodies get a small limit instead of Kestrel's default of about 30 MB.
    /// A passkey credential is a few kilobytes.
    /// </summary>
    internal static class AdminBodyLimit
    {
        public const int MaxBytes = 16 * 1024;

        public static IApplicationBuilder UseAdminBodyLimit(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments("/bff/admin", StringComparison.OrdinalIgnoreCase))
                {
                    await next(context);
                    return;
                }

                // A body without a declared length (chunked) is cut off by the server while it is read.
                var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (feature is { IsReadOnly: false })
                {
                    feature.MaxRequestBodySize = MaxBytes;
                }

                if (context.Request.ContentLength > MaxBytes)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    await context.Response.WriteAsJsonAsync(
                        new ProblemDetails { Status = StatusCodes.Status413PayloadTooLarge, Title = "The request is too large" },
                        (System.Text.Json.JsonSerializerOptions?)null,
                        "application/problem+json",
                        context.RequestAborted);
                    return;
                }

                await next(context);
            });
    }
}
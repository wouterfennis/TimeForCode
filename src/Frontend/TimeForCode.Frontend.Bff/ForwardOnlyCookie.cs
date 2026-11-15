using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Lets a route forward one named cookie and drops every other, so an API receives only the credential it needs.
    /// The Authorization route uses it for the refresh token cookie that refresh and logout read; the access token
    /// cookie never travels upstream as a cookie because the bearer handler turns it into the <c>Authorization</c> header.
    /// A route opts in with <c>"ForwardCookie": "&lt;name&gt;"</c> in its metadata; routes without it get the
    /// <c>RequestHeaderRemove</c> transform from appsettings.json instead.
    /// </summary>
    internal static class ForwardOnlyCookie
    {
        private const string MetadataKey = "ForwardCookie";
        private const string CookieHeader = "Cookie";

        public static void Add(TransformBuilderContext context)
        {
            string? name = null;
            if (context.Route.Metadata is null || !context.Route.Metadata.TryGetValue(MetadataKey, out name))
            {
                return;
            }

            context.AddRequestTransform(transform =>
            {
                transform.ProxyRequest.Headers.Remove(CookieHeader);
                if (transform.HttpContext.Request.Cookies.TryGetValue(name, out var value))
                {
                    transform.ProxyRequest.Headers.TryAddWithoutValidation(CookieHeader, $"{name}={Uri.EscapeDataString(value)}");
                }

                return ValueTask.CompletedTask;
            });
        }
    }
}
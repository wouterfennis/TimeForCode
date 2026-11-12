using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Maps an API's own absolute URL in a <c>Location</c> header (for example the address of a created project)
    /// back to the route the browser uses: <c>http://donation-api:8080/api/v1/Project/42</c> becomes
    /// <c>/api/donation/v1/Project/42</c>. Other locations, such as the GitHub login or the SPA, stay as they are.
    /// The route's prefix comes from its <c>PublicPathPrefix</c> metadata in appsettings.json.
    /// </summary>
    internal static class LocationRewrite
    {
        private const string MetadataKey = "PublicPathPrefix";
        private const string ApiPath = "/api/";

        public static void Add(TransformBuilderContext context)
        {
            string? prefix = null;
            if (context.Route.Metadata is null || !context.Route.Metadata.TryGetValue(MetadataKey, out prefix))
            {
                return;
            }

            context.AddResponseTransform(transform =>
            {
                var headers = transform.HttpContext.Response.Headers;
                var destination = transform.HttpContext.GetReverseProxyFeature().ProxiedDestination?.Model.Config.Address;
                if (Uri.TryCreate(destination, UriKind.Absolute, out var api)
                    && Uri.TryCreate(headers.Location.ToString(), UriKind.Absolute, out var location))
                {
                    headers.Location = Rewrite(location, api, prefix);
                }

                return ValueTask.CompletedTask;
            });
        }

        private static string Rewrite(Uri location, Uri api, string prefix)
        {
            var isApisOwnPath = Uri.Compare(location, api, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0
                && location.AbsolutePath.StartsWith(ApiPath, StringComparison.Ordinal);
            return isApisOwnPath
                ? prefix + location.PathAndQuery[ApiPath.Length..] + location.Fragment
                : location.OriginalString;
        }
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace TimeForCode.Shared.Api.Extensions
{
    /// <summary>
    /// Takes the original scheme, host and client address from <c>X-Forwarded-*</c> headers, but only from proxies the
    /// deployment names. Without this, a service behind a TLS-terminating proxy sees plain HTTP, so HSTS and
    /// <c>Secure</c> cookie handling never apply, and any client could otherwise spoof the headers.
    /// </summary>
    /// <remarks>
    /// Configuration section <c>ForwardedHeaders</c>: <c>KnownProxies</c> (IP addresses) and <c>KnownNetworks</c> (CIDR ranges).
    /// When neither is set, only the loopback address is trusted (the ASP.NET Core default), so a request from any other
    /// address cannot change the scheme or host.
    /// </remarks>
    public static class ForwardedHeadersExtensions
    {
        public const string SectionName = "ForwardedHeaders";

        /// <summary>
        /// Registers the forwarded-headers options with the trusted proxies from configuration.
        /// </summary>
        public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
        {
            var proxies = configuration.GetSection($"{SectionName}:KnownProxies").Get<List<string>>() ?? [];
            var networks = configuration.GetSection($"{SectionName}:KnownNetworks").Get<List<string>>() ?? [];

            return services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

                if (proxies.Count == 0 && networks.Count == 0)
                {
                    return;
                }

                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
                foreach (var proxy in proxies)
                {
                    options.KnownProxies.Add(Parse(proxy, "KnownProxies", IPAddress.Parse));
                }

                foreach (var network in networks)
                {
                    options.KnownIPNetworks.Add(Parse(network, "KnownNetworks", System.Net.IPNetwork.Parse));
                }
            });
        }

        private static T Parse<T>(string value, string setting, Func<string, T> parse)
        {
            try
            {
                return parse(value);
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException($"{SectionName}:{setting} contains '{value}', which is not valid.", exception);
            }
        }
    }
}
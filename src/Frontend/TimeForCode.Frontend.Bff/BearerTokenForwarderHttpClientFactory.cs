using TimeForCode.Shared.Api.Handlers;
using Yarp.ReverseProxy.Forwarder;

namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// YARP's client for forwarded calls, with the shared <see cref="CookieAuthorizationHandler"/> in front:
    /// it turns the HttpOnly access-token cookie into the bearer header the APIs require.
    /// </summary>
    internal class BearerTokenForwarderHttpClientFactory(IHttpContextAccessor httpContextAccessor) : ForwarderHttpClientFactory
    {
        protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
            => new CookieAuthorizationHandler(httpContextAccessor) { InnerHandler = base.WrapHandler(context, handler) };
    }
}
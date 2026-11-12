using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>
    /// The production client factory with the network transport swapped for <see cref="UpstreamStub"/>,
    /// so the BFF's own handlers (the bearer token) still run.
    /// </summary>
    internal sealed class StubTransportForwarderHttpClientFactory(IHttpContextAccessor httpContextAccessor, UpstreamStub upstream)
        : BearerTokenForwarderHttpClientFactory(httpContextAccessor)
    {
        protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
            => base.WrapHandler(context, upstream);
    }
}
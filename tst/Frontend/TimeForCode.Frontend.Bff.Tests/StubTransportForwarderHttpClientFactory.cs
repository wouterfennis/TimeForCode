using Yarp.ReverseProxy.Forwarder;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>
    /// The production client factory with the network transport swapped for <see cref="UpstreamStub"/>.
    /// </summary>
    internal sealed class StubTransportForwarderHttpClientFactory(UpstreamStub upstream) : ForwarderHttpClientFactory
    {
        protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
            => base.WrapHandler(context, upstream);
    }
}
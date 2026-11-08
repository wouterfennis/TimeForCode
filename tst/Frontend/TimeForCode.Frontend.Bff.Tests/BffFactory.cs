using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>
    /// Starts the BFF in memory with the SPA fixture files from <c>TestData/wwwroot</c>; forwarded API calls go to <see cref="Upstream"/>.
    /// </summary>
    public class BffFactory : WebApplicationFactory<Program>
    {
        public UpstreamStub Upstream { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.WebRootKey, Path.Combine(AppContext.BaseDirectory, "TestData", "wwwroot"));
            builder.ConfigureTestServices(services => services.AddSingleton<IForwarderHttpClientFactory>(provider =>
                new StubTransportForwarderHttpClientFactory(provider.GetRequiredService<IHttpContextAccessor>(), Upstream)));
        }

        public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
    }
}
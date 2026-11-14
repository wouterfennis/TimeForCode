using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeForCode.Frontend.Bff.Sessions;
using Yarp.ReverseProxy.Forwarder;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>
    /// Starts the BFF in memory with the SPA fixture files from <c>TestData/wwwroot</c>. Forwarded API calls go to <see cref="Upstream"/>,
    /// the Authorization API's token endpoints are <see cref="Api"/>, sessions live in <see cref="Store"/> and time is <see cref="Time"/>.
    /// </summary>
    public class BffFactory : WebApplicationFactory<Program>
    {
        public const string PublicAuthorizationAddress = "https://auth.example.test";

        public BffFactory()
        {
            Time = new ManualTimeProvider();
            Api = new FakeAuthorizationApi(Time);
        }

        public UpstreamStub Upstream { get; } = new();

        internal ManualTimeProvider Time { get; }

        internal FakeAuthorizationApi Api { get; }

        internal InMemorySessionStore Store { get; } = new();

        internal LogCapture Logs { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.WebRootKey, Path.Combine(AppContext.BaseDirectory, "TestData", "wwwroot"));
            builder.UseSetting("Authorization:PublicBaseAddress", PublicAuthorizationAddress);
            builder.UseSetting("Authorization:BaseAddress", "http://authorization-api.test");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IForwarderHttpClientFactory>(new StubTransportForwarderHttpClientFactory(Upstream));
                services.AddSingleton<TimeProvider>(Time);
                services.AddSingleton<ISessionStore>(Store);
                services.AddSingleton<IAuthorizationApi>(Api);
                services.AddSingleton<ILoggerProvider>(Logs);
            });
        }

        public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
    }
}
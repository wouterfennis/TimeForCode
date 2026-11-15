using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Shared.Tests.Api
{
    [TestClass]
    public class ForwardedHeadersTests
    {
        public TestContext TestContext { get; set; } = null!;

        private static async Task<string> SchemeHostAndClientAsync(Dictionary<string, string?> settings, string proxyAddress, CancellationToken cancellationToken)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            using var host = await new HostBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services => services.AddTrustedForwardedHeaders(configuration))
                    .Configure(app =>
                    {
                        app.Use((context, next) =>
                        {
                            context.Connection.RemoteIpAddress = IPAddress.Parse(proxyAddress);
                            return next();
                        });
                        app.UseForwardedHeaders();
                        app.Run(context => context.Response.WriteAsync($"{context.Request.Scheme} {context.Request.Host} {context.Connection.RemoteIpAddress}", cancellationToken));
                    }))
                .StartAsync(cancellationToken);

            using var client = host.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("X-Forwarded-Proto", "https");
            request.Headers.Add("X-Forwarded-Host", "api.example.com");
            request.Headers.Add("X-Forwarded-For", "203.0.113.7");
            var response = await client.SendAsync(request, cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        [TestMethod]
        public async Task UseForwardedHeaders_RequestFromAConfiguredProxy_TrustsSchemeHostAndClient()
        {
            var settings = new Dictionary<string, string?> { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" };

            var result = await SchemeHostAndClientAsync(settings, "10.0.0.5", TestContext.CancellationToken);

            result.Should().Be("https api.example.com 203.0.113.7");
        }

        [TestMethod]
        public async Task UseForwardedHeaders_RequestFromAConfiguredNetwork_TrustsSchemeHostAndClient()
        {
            var settings = new Dictionary<string, string?> { ["ForwardedHeaders:KnownNetworks:0"] = "10.1.0.0/16" };

            var result = await SchemeHostAndClientAsync(settings, "10.1.2.3", TestContext.CancellationToken);

            result.Should().Be("https api.example.com 203.0.113.7");
        }

        [TestMethod]
        public async Task UseForwardedHeaders_RequestFromAnUntrustedAddress_IgnoresTheForwardedHeaders()
        {
            var settings = new Dictionary<string, string?> { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" };

            var result = await SchemeHostAndClientAsync(settings, "10.0.0.9", TestContext.CancellationToken);

            result.Should().Be("http localhost 10.0.0.9");
        }

        [TestMethod]
        public async Task UseForwardedHeaders_NoProxyConfigured_IgnoresForwardedHeadersFromARemoteAddress()
        {
            var result = await SchemeHostAndClientAsync([], "10.0.0.5", TestContext.CancellationToken);

            result.Should().Be("http localhost 10.0.0.5");
        }

        [TestMethod]
        [DataRow("ForwardedHeaders:KnownProxies:0", "not-an-ip", "KnownProxies")]
        [DataRow("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/99", "KnownNetworks")]
        public async Task AddTrustedForwardedHeaders_MalformedValue_FailsAndNamesTheSetting(string key, string value, string setting)
        {
            var act = () => SchemeHostAndClientAsync(new Dictionary<string, string?> { [key] = value }, "10.0.0.5", TestContext.CancellationToken);

            (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage($"*{setting}*{value}*");
        }
    }
}
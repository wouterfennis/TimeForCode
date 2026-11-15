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
    public class DefaultApiPipelineHstsTests
    {
        public TestContext TestContext { get; set; } = null!;

        private async Task<HttpResponseMessage> GetThroughProxyAsync(string environment, string proxyAddress, bool forwardedHttps)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" })
                .Build();
            var host = await new HostBuilder()
                .UseEnvironment(environment)
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddDefaultControllers();
                        services.AddRateLimiter(_ => { });
                        services.AddAuthentication();
                        services.AddAuthorization();
                        services.AddTrustedForwardedHeaders(configuration);
                    })
                    .Configure(app =>
                    {
                        app.Use((context, next) =>
                        {
                            context.Connection.RemoteIpAddress = IPAddress.Parse(proxyAddress);
                            return next();
                        });
                        app.UseDefaultApiPipeline(app.ApplicationServices.GetRequiredService<IWebHostEnvironment>());
                    }))
                .StartAsync(TestContext.CancellationToken);

            var client = host.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            if (forwardedHttps)
            {
                request.Headers.Add("X-Forwarded-Proto", "https");
                request.Headers.Add("X-Forwarded-Host", "api.example.com");
            }

            return await client.SendAsync(request, TestContext.CancellationToken);
        }

        [TestMethod]
        public async Task Get_ForwardedHttpsFromTheTrustedProxyInProduction_CarriesStrictTransportSecurity()
        {
            var response = await GetThroughProxyAsync("Production", "10.0.0.5", forwardedHttps: true);

            response.Headers.Contains("Strict-Transport-Security").Should().BeTrue();
        }

        [TestMethod]
        public async Task Get_ForwardedHttpsFromAnUntrustedAddress_CarriesNoStrictTransportSecurity()
        {
            var response = await GetThroughProxyAsync("Production", "10.0.0.9", forwardedHttps: true);

            response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
        }

        [TestMethod]
        public async Task Get_ForwardedHttpsInDevelopment_CarriesNoStrictTransportSecurity()
        {
            var response = await GetThroughProxyAsync("Development", "10.0.0.5", forwardedHttps: true);

            response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
        }
    }
}
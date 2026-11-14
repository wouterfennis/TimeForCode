using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class ForwardedHeadersTests
    {
        public TestContext TestContext { get; set; } = null!;

        private sealed class RemoteAddressFilter(string address) : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use((context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(address);
                    return nextMiddleware();
                });
                next(app);
            };
        }

        private async Task<HttpResponseMessage> GetThroughProxyAsync(string environment, string proxyAddress)
        {
            using var factory = new BffFactory().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.0.0.5");
                builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(new RemoteAddressFilter(proxyAddress)));
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("X-Forwarded-Proto", "https");
            request.Headers.Add("X-Forwarded-Host", "app.example.com");

            return await client.SendAsync(request, TestContext.CancellationToken);
        }

        [TestMethod]
        public async Task Get_ForwardedHttpsFromTheTrustedProxyInProduction_CarriesStrictTransportSecurity()
        {
            var response = await GetThroughProxyAsync("Production", "10.0.0.5");

            response.Headers.Contains("Strict-Transport-Security").Should().BeTrue();
        }

        [TestMethod]
        public async Task Get_ForwardedHttpsFromAnUntrustedAddress_CarriesNoStrictTransportSecurity()
        {
            var response = await GetThroughProxyAsync("Production", "10.0.0.9");

            response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
        }

        [TestMethod]
        public async Task Get_InDevelopment_CarriesNoStrictTransportSecurity()
        {
            var response = await GetThroughProxyAsync("Development", "10.0.0.5");

            response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
        }
    }
}
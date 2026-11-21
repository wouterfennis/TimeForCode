using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Shared.Tests.Api
{
    [TestClass]
    public class DefaultApiPipelineRootRedirectTests
    {
        public TestContext TestContext { get; set; } = null!;

        private async Task<HttpClient> StartAsync()
        {
            var host = await new HostBuilder()
                .UseEnvironment("Production")
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddDefaultControllers();
                        services.AddOpenApi();
                        services.AddRateLimiter(_ => { });
                        services.AddAuthentication();
                        services.AddAuthorization();
                    })
                    .Configure(app => app.UseDefaultApiPipeline(app.ApplicationServices.GetRequiredService<IWebHostEnvironment>())))
                .StartAsync(TestContext.CancellationToken);

            return host.GetTestClient();
        }

        [TestMethod]
        public async Task Get_Root_RedirectsTemporarilyToTheApiReference()
        {
            var client = await StartAsync();

            var response = await client.GetAsync("/", TestContext.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location!.OriginalString.Should().Be("/scalar/v1");
        }

        [TestMethod]
        public async Task Get_RootRedirectTarget_ReturnsSuccess()
        {
            var client = await StartAsync();
            var redirect = await client.GetAsync("/", TestContext.CancellationToken);

            var response = await client.GetAsync(redirect.Headers.Location, TestContext.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task Get_OpenApiDocument_DoesNotListTheRootRedirect()
        {
            var client = await StartAsync();

            var document = await client.GetStringAsync("/openapi/v1.json", TestContext.CancellationToken);

            System.Text.Json.JsonDocument.Parse(document).RootElement.GetProperty("paths").TryGetProperty("/", out _).Should().BeFalse();
        }
    }
}
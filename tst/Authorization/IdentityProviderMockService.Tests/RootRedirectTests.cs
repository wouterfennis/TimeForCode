using AwesomeAssertions;
using IdentityProviderMockService.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace IdentityProviderMockService.Tests
{
    [TestClass]
    public class RootRedirectTests
    {
        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public async Task Get_Root_RedirectsTemporarilyToTheApiReference()
        {
            using var factory = new WebApplicationFactory<LoginController>();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            using var response = await client.GetAsync("/", TestContext.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location!.OriginalString.Should().Be("/scalar/v1");
        }

        [TestMethod]
        public async Task Get_RootRedirectTarget_ReturnsSuccess()
        {
            using var factory = new WebApplicationFactory<LoginController>();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            using var response = await client.GetAsync("/scalar/v1", TestContext.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
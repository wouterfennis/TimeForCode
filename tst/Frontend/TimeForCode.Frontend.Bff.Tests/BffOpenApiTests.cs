using AwesomeAssertions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class BffOpenApiTests
    {
        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public async Task Describe_GeneratedDocument_GivesTheBffResponsesTheirOwnDescriptions()
        {
            using var factory = new BffFactory();
            var provider = factory.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");

            var document = await provider.GetOpenApiDocumentAsync(TestContext.CancellationToken);

            var responses = document.Paths!["/bff/session"].Operations!.Values.Single().Responses!;
            responses["200"].Description.Should().Contain("never holds a token");
            responses["401"].Description.Should().Contain("Nobody is signed in");
            document.Paths["/bff/logout"].Operations!.Values.Single().Responses!["403"].Description.Should().Contain("X-Requested-With");
        }
    }
}
using AwesomeAssertions;

namespace TimeForCode.Reporting.Api.Tests
{
    [TestClass]
    public class SwaggerTests : VerifyBase
    {
        [TestMethod]
        public async Task Verify_GeneratedSwaggerFile_ShouldNotChangeUnlessIntended()
        {
            // Arrange
            var swagger = await File.ReadAllTextAsync("TimeForCode.Reporting.Api.json", CancellationToken.None);
            // The JSON escapes line breaks from XML doc comments, so Verify cannot normalize them; keep the snapshot OS-independent.
            swagger = swagger.Replace("\\r\\n", "\\n");

            // Act

            // Assert
            swagger.Should().NotBeNull();
            await Verify(swagger);
        }
    }
}
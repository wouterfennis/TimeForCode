using FluentAssertions;

namespace TimeForCode.Authorization.Api.Tests
{
    // write test to check if the dependencies are registered correctly

    [TestClass]
    public class SwaggerTests : VerifyBase
    {
        [TestMethod]
        public async Task Verify_GeneratedSwaggerFile_ShouldNotChangeUnlessIntended()
        {
            // Arrange
            var swagger = await File.ReadAllTextAsync("swagger.json");

            // Act

            // Assert
            swagger.Should().NotBeNull();
            await Verify(swagger);
        }
    }
}
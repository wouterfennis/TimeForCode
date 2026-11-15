using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace TimeForCode.Authorization.Api.Tests
{
    [TestClass]
    public class StartupTests
    {
        [TestMethod]
        [DataRow("0")]
        [DataRow("16")]
        public void ConfigureServices_WithAnAccessTokenLifetimeOutsideTheAllowedRange_FailsFast(string minutes)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TokenCreationOptions:TokenExpiresInMinutes"] = minutes,
                })
                .Build();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns("Production");
            var startup = new Startup(configuration, environment.Object);

            // Act
            var act = () => startup.ConfigureServices(new ServiceCollection());

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*TokenExpiresInMinutes*");
        }
    }
}
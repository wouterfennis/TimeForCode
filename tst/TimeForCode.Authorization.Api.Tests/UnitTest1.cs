using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeForCode.Authorization.Api.Controllers;
using TimeForCode.Authorization.Application.Handlers;

namespace TimeForCode.Authorization.Api.Tests
{
    // write test to check if the dependencies are registered correctly

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Test1()
        {
            // Arrange
            var services = new ServiceCollection();
            var startup = new Startup(new ConfigurationBuilder().Build());

            // Act
            startup.ConfigureServices(services);

            // Assert
            var result = services.BuildServiceProvider().GetRequiredService<LoginHandler>();
            Assert.IsNotNull(result);
        }
    }
}
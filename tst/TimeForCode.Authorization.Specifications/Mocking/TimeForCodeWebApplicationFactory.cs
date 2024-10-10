
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TimeForCode.Authorization.Api;

namespace TimeForCode.Authorization.Specifications.Mocking
{
    public class TimeForCodeWebApplicationFactory : WebApplicationFactory<Startup>
    {
        //protected override void ConfigureWebHost(IWebHostBuilder builder)
        //{
        //    builder.ConfigureServices(services =>
        //    {
        //        // Remove the existing service registration if necessary
        //        //var descriptor = services.SingleOrDefault(
        //        //    d => d.ServiceType == typeof(IMyService));
        //        //if (descriptor != null)
        //        //{
        //        //    services.Remove(descriptor);
        //        //}

        //        //// Add a mock service
        //        //var mockService = new Mock<IMyService>();
        //        //mockService.Setup(service => service.DoWork()).Returns("Mocked result");

        //        //services.AddSingleton(mockService.Object);
        //    });
        //}
    }

}

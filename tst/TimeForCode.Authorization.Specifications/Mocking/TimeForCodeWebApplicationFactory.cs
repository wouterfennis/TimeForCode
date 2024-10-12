
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using TimeForCode.Authorization.Api;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Infrastructure.Services;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Specifications.Mocking
{
    public class TimeForCodeWebApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var interfacesToRemove = new List<Type>
                {
                    typeof(IIdentityProviderService),
                    typeof(IIdentityProviderServiceFactory)
                };

                var descriptors = services
                    .Where(d => interfacesToRemove.Contains(d.ServiceType))
                    .ToList();

                if (descriptors != null)
                {
                    descriptors.ForEach(d => services.Remove(d));
                }

                var mockIdentityProviderService = new Mock<IIdentityProviderService>();
                var mockIdentityProviderServiceFactory = new Mock<IIdentityProviderServiceFactory>();

                mockIdentityProviderServiceFactory.Setup(x => x.GetIdentityProviderService(It.IsAny<IdentityProvider>()))
                    .Returns(Result<IIdentityProviderService>.Success(mockIdentityProviderService.Object));

                services.TryAddSingleton(mockIdentityProviderService);
                services.TryAddSingleton(mockIdentityProviderServiceFactory.Object);
            });
        }
    }

}

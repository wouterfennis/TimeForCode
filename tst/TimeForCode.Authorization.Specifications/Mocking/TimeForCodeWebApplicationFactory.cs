
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Moq;
using System.Runtime.CompilerServices;
using TimeForCode.Authorization.Api;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;
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
                    typeof(IIdentityProviderServiceFactory),
                    typeof(IRandomGenerator),
                    typeof(IMongoDbContext)
                };

                var descriptors = services
                    .Where(d => interfacesToRemove.Contains(d.ServiceType))
                    .ToList();

                if (descriptors.Count > 0)
                {
                    descriptors.ForEach(d => services.Remove(d));
                }

                var mockIdentityProviderService = new Mock<IIdentityProviderService>();
                var mockIdentityProviderServiceFactory = new Mock<IIdentityProviderServiceFactory>();
                var mockRandomGenerator = new Mock<IRandomGenerator>();
                MockMongoDb(services);

                mockIdentityProviderServiceFactory.Setup(x => x.GetIdentityProviderService(It.IsAny<IdentityProvider>()))
                    .Returns(Result<IIdentityProviderService>.Success(mockIdentityProviderService.Object));

                mockRandomGenerator.Setup(x => x.GenerateRandomString())
                    .Returns(Constants.StateKey);

                services.TryAddSingleton(mockIdentityProviderService);
                services.TryAddSingleton(mockIdentityProviderServiceFactory.Object);
                services.TryAddSingleton(mockRandomGenerator.Object);
            });

        }

        private static void MockMongoDb(IServiceCollection services)
        {
            var mockUserCollection = new Mock<IMongoCollection<AccountInformation>>();
            var mockDbContext = new Mock<IMongoDbContext>();

            mockDbContext.Setup(c => c.GetCollection<AccountInformation>())
                .Returns(mockUserCollection.Object);

            services.TryAddSingleton(mockUserCollection);
            services.TryAddSingleton(mockDbContext.Object);
        }
    }

}
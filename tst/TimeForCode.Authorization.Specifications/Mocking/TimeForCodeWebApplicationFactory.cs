
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Moq;
using RestSharp;
using RichardSzalay.MockHttp;
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
                    typeof(IRandomGenerator),
                    typeof(IMongoDbContext),
                    typeof(RestClient)
                };

                var descriptors = services
                    .Where(d => interfacesToRemove.Contains(d.ServiceType))
                    .ToList();

                if (descriptors.Count > 0)
                {
                    descriptors.ForEach(d => services.Remove(d));
                }

                var mockRandomGenerator = new Mock<IRandomGenerator>();
                MockMongoDb(services);

                mockRandomGenerator.Setup(x => x.GenerateRandomString())
                    .Returns(Constants.StateKey);

                var mockHttp = new MockHttpMessageHandler();
                services.TryAddSingleton(mockHttp);

                var restClient = new RestClient(new RestClientOptions { ConfigureMessageHandler = _ => mockHttp });

                services.TryAddSingleton(restClient);
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
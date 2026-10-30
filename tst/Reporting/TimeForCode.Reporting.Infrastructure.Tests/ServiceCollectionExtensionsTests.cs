using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Infrastructure.Extensions;
using TimeForCode.Reporting.Infrastructure.Services;

namespace TimeForCode.Reporting.Infrastructure.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        private static IConfiguration Configuration(string? baseUri) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(baseUri == null ? [] : new Dictionary<string, string?> { ["DonationApiOptions:BaseUri"] = baseUri })
                .Build();

        [TestMethod]
        public void AddInfrastructureLayer_WithTheDonationApiAddress_RegistersTheDonationDataSource()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddInfrastructureLayer(Configuration("http://donation-api:8080"));

            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<IDonationDataSource>().Should().BeOfType<DonationApiDataSource>();
        }

        [TestMethod]
        public void AddInfrastructureLayer_PointsTheDonationClientAtTheConfiguredAddress()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructureLayer(Configuration("http://donation-api:8080"));

            using ServiceProvider provider = services.BuildServiceProvider();
            using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(DonationApiDataSource.HttpClientName);

            client.BaseAddress.Should().Be(new Uri("http://donation-api:8080"));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("not a uri")]
        public void CreateClient_WithoutAUsableDonationApiAddress_RefusesWithAClearMessage(string? baseUri)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructureLayer(Configuration(baseUri));
            using ServiceProvider provider = services.BuildServiceProvider();
            IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

            Func<HttpClient> create = () => factory.CreateClient(DonationApiDataSource.HttpClientName);

            create.Should().Throw<InvalidOperationException>().WithMessage("*DonationApiOptions:BaseUri*");
        }
    }
}
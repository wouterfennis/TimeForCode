using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Infrastructure.Options;
using TimeForCode.Reporting.Infrastructure.Services;

namespace TimeForCode.Reporting.Infrastructure.Extensions
{
    [ExcludeFromCodeCoverage(Justification = "DI registration")]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<DonationApiOptions>()
                .Configure(options => options.BaseUri = configuration[$"{DonationApiOptions.SectionName}:{nameof(DonationApiOptions.BaseUri)}"]);

            // The address is checked when the client is created, not at host start: building the host to generate the OpenAPI document must not need it.
            services.AddHttpClient(DonationApiDataSource.HttpClientName)
                .ConfigureHttpClient((provider, client) =>
                {
                    string? baseUri = provider.GetRequiredService<IOptions<DonationApiOptions>>().Value.BaseUri;
                    if (!Uri.TryCreate(baseUri, UriKind.Absolute, out Uri? address))
                    {
                        throw new InvalidOperationException($"{DonationApiOptions.SectionName}:{nameof(DonationApiOptions.BaseUri)} must be the absolute address of the Donation API.");
                    }

                    client.BaseAddress = address;
                });
            services.AddScoped<IDonationDataSource, DonationApiDataSource>();

            return services;
        }
    }
}
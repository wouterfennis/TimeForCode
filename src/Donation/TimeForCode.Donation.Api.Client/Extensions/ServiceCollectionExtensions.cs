using Microsoft.Extensions.DependencyInjection;
using TimeForCode.Shared.Api.Handlers;

namespace TimeForCode.Donation.Api.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDonationClient(this IServiceCollection services, Uri baseAddress)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<CookieAuthorizationHandler>();
            services.AddHttpClient<IDonationClient, DonationClient>((client) =>
            {
                client.BaseAddress = baseAddress;
            })
            .AddHttpMessageHandler<CookieAuthorizationHandler>();

            return services;
        }
    }
}
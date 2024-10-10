using Microsoft.Extensions.DependencyInjection;

namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthClient(this IServiceCollection services, Uri baseAddress)
        {
            services.AddHttpClient<IAuthClient, AuthClient>((client) =>
            {
                client.BaseAddress = baseAddress;
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    AllowAutoRedirect = false // Prevent automatic redirect follow
                };
            });

            return services;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using TimeForCode.Shared.Api.Handlers;

namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthClient(this IServiceCollection services, Uri baseAddress)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<CookieAuthorizationHandler>();
            services.AddHttpClient<IAuthClient, AuthClient>((client) =>
            {
                client.BaseAddress = baseAddress;
            })
            .AddHttpMessageHandler<CookieAuthorizationHandler>();

            return services;
        }
    }
}
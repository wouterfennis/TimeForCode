using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Options;

namespace TimeForCode.Authorization.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the application layer services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance used to configure the services.</param>
        /// <returns>The <see cref="IServiceCollection"/> with the application layer services added.</returns>
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<LoginHandler>());

            services
                .Configure<ExternalIdentityProviderOptions>(options => configuration.GetSection(ExternalIdentityProviderOptions.SectionName)
                .Bind(options));

            return services;
        }
    }
}

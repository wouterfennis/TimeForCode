using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Infrastructure.Random;
using TimeForCode.Authorization.Infrastructure.Services;

namespace TimeForCode.Authorization.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the infrastructure layer services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> with the application layer services added.</returns>
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services)
        {
            services.AddScoped<IIdentityProviderServiceFactory, IdentityProviderServiceFactory>();
            services.AddScoped<IRandomGenerator, RandomGenerator>();
            services.AddScoped<IIdentityProviderService, GithubService>();
            return services;
        }
    }
}

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Reporting.Application.Handlers;

namespace TimeForCode.Reporting.Application.Extensions
{
    [ExcludeFromCodeCoverage(Justification = "DI registration")]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<IAnchor>());
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<BadgeAwarder>();

            return services;
        }
    }
}
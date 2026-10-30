using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Reporting.Application.Extensions
{
    [ExcludeFromCodeCoverage(Justification = "DI registration")]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<IAnchor>());

            return services;
        }
    }
}
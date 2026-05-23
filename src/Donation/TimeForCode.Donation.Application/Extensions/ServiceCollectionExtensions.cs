using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TimeForCode.Donation.Application.Handlers;

namespace TimeForCode.Donation.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RegisterProjectHandler>());
            return services;
        }
    }
}
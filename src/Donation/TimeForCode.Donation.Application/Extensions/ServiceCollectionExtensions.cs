using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Shared.Behaviours;

namespace TimeForCode.Donation.Application.Extensions
{
    [ExcludeFromCodeCoverage(Justification = "DI registration")]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<RegisterProjectHandler>();
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            });

            services.AddValidatorsFromAssemblyContaining<RegisterProjectCommandValidator>(ServiceLifetime.Transient);

            return services;
        }
    }
}
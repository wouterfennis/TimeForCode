using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Application.Validators;
using TimeForCode.Shared.Behaviours;

namespace TimeForCode.Authorization.Application.Extensions
{
    [ExcludeFromCodeCoverage(Justification = "DI registration")]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the application layer services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance used to configure the services.</param>
        /// <param name="allowEphemeralSigningKey">Whether a missing signing certificate may be replaced by a throwaway key (Development only).</param>
        /// <returns>The <see cref="IServiceCollection"/> with the application layer services added.</returns>
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services, IConfiguration configuration, bool allowEphemeralSigningKey)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<LoginHandler>();
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            });

            services.AddValidatorsFromAssemblyContaining<LoginCommandValidator>(ServiceLifetime.Transient);

            services
                .Configure<AuthenticationOptions>(options => configuration.GetSection(AuthenticationOptions.SectionName)
                .Bind(options));

            services
                .Configure<TokenCreationOptions>(options => configuration.GetSection(TokenCreationOptions.SectionName)
                .Bind(options));

            services
                .Configure<ExternalIdentityProviderOptions>(options => configuration.GetSection(ExternalIdentityProviderOptions.SectionName)
                .Bind(options));

            services
                .Configure<AdminPasskeyOptions>(options => configuration.GetSection(AdminPasskeyOptions.SectionName)
                .Bind(options));

            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(TestAdminAccessOptions.Bind(configuration)));

            ConfigureSigningCertificate(services, configuration, allowEphemeralSigningKey);

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            return services;
        }

        private static void ConfigureSigningCertificate(IServiceCollection services, IConfiguration configuration, bool allowEphemeralSigningKey)
        {
            services.AddSingleton(SigningKeySet.Load(configuration, allowEphemeralSigningKey));
        }
    }
}
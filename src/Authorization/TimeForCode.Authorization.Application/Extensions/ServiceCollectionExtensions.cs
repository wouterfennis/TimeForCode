using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        /// <returns>The <see cref="IServiceCollection"/> with the application layer services added.</returns>
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services, IConfiguration configuration)
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

            ConfigureSigningCertificate(services, configuration);

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            return services;
        }

        private static void ConfigureSigningCertificate(IServiceCollection services, IConfiguration configuration)
        {
            var rsa = LoadCertificate(configuration);
            services.AddSingleton(rsa);
        }

        public static RSA LoadCertificate(IConfiguration configuration)
        {
            var rsaOptions = RsaOptions.Bind(configuration);

            var certificateBytes = Convert.FromBase64String(rsaOptions.Base64Certificate);

            if (certificateBytes == null || certificateBytes.Length == 0)
            {
                return RSA.Create();
            }

            var certificate = X509CertificateLoader.LoadPkcs12(certificateBytes, (string?)null);
            return certificate.GetRSAPrivateKey()!;
        }
    }
}
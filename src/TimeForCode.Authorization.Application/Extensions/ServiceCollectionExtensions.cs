using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;

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
                .Configure<AuthenticationOptions>(options => configuration.GetSection(AuthenticationOptions.SectionName)
                .Bind(options));

            services
                .Configure<ExternalIdentityProviderOptions>(options => configuration.GetSection(ExternalIdentityProviderOptions.SectionName)
                .Bind(options));

            ConfigureSigningCertificate(services, configuration);

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }

        private static void ConfigureSigningCertificate(IServiceCollection services, IConfiguration configuration)
        {
            var rsa = LoadCertificate(configuration);
            services.AddSingleton(rsa);
        }

        private static RSA LoadCertificate(IConfiguration configuration)
        {
            var rsaOptions = new RsaOptions();
            configuration.GetSection(RsaOptions.SectionName).Bind(rsaOptions);

            var certificateBytes = Convert.FromBase64String(rsaOptions.Base64Certificate);

            if (certificateBytes == null || certificateBytes.Length == 0)
            {
                return RSA.Create();
            }

            var certificate = new X509Certificate2(certificateBytes, (string?)null);
            return certificate.GetRSAPrivateKey()!;
        }
    }
}
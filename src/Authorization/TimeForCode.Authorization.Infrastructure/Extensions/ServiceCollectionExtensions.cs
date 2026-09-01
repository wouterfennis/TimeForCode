using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using RestSharp;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Infrastructure.Options;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;
using TimeForCode.Authorization.Infrastructure.Persistence.Database.Admin;
using TimeForCode.Authorization.Infrastructure.Persistence.Memory;
using TimeForCode.Authorization.Infrastructure.Services;
using TimeForCode.Authorization.Infrastructure.Services.Admin;
using TimeForCode.Authorization.Infrastructure.Services.Github;

namespace TimeForCode.Authorization.Infrastructure.Extensions
{
    [ExcludeFromCodeCoverage(Justification = "DI registration")]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the infrastructure layer services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> with the application layer services added.</returns>
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IIdentityProviderServiceFactory, IdentityProviderServiceFactory>();
            services.AddScoped<IRandomGenerator, RandomGenerator>();
            services.AddScoped<IIdentityProviderService, GithubService>();
            services.AddScoped<IGithubApiService, GithubService>();

            services.AddSingleton(TimeProvider.System);

            services
                .Configure<DbOptions>(options => configuration.GetSection(DbOptions.SectionName)
                .Bind(options));

            services.AddSingleton<RestClient>();
            services.AddSingleton<IMongoDbContext, MongoDbContext>();
            services.AddScoped<IAccountInformationRepository, AccountInformationRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddMemoryCache();
            services.AddSingleton<IStateRepository, StateRepository>();

            services.AddDataProtection();
            services.AddScoped<IEncryptionService, DataProtectionEncryptionService>();

            AddAdminPasskeySlice(services, configuration);

            return services;
        }

        /// <summary>
        /// Wires up the admin WebAuthn passkey slice. Isolated in its own method so it can be removed
        /// wholesale without touching the GitHub flow registrations above.
        /// </summary>
        private static void AddAdminPasskeySlice(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAdminCredentialRepository, AdminCredentialRepository>();

            services.AddHttpContextAccessor();

            services.AddIdentityCore<AdminPasskeyUser>()
                .AddUserStore<AdminUserStore>();
            services.AddScoped<IUserPasskeyStore<AdminPasskeyUser>, AdminUserStore>();
            services.AddScoped(typeof(IPasskeyHandler<>), typeof(PasskeyHandler<>));

            var adminPasskeyOptions = AdminPasskeyOptions.Bind(configuration);
            services.Configure<IdentityPasskeyOptions>(options =>
            {
                options.ServerDomain = adminPasskeyOptions.ServerDomain;
            });

            services.AddScoped<IPasskeyCeremonyService, PasskeyCeremonyService>();
        }
    }
}
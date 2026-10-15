using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;

namespace TimeForCode.Authorization.Infrastructure.Extensions
{
    internal static class DataProtectionExtensions
    {
        internal const string ApplicationName = "TimeForCode.Authorization";

        /// <summary>
        /// Data Protection with a key ring that is persisted in the database and shared by all instances,
        /// so protected values survive restarts and can be read by any instance.
        /// </summary>
        internal static IServiceCollection AddSharedKeyRingDataProtection(this IServiceCollection services)
        {
            services.AddDataProtection().SetApplicationName(ApplicationName);
            services.AddSingleton<IXmlRepository, MongoXmlRepository>();
            services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(provider =>
                new ConfigureOptions<KeyManagementOptions>(options => options.XmlRepository = provider.GetRequiredService<IXmlRepository>()));

            return services;
        }
    }
}
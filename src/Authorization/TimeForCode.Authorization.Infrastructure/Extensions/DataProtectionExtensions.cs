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

        /// <summary>Entry assembly name of the OpenAPI document generator that runs the app at build time.</summary>
        internal const string OpenApiDocumentGeneratorAssembly = "GetDocument.Insider";

        /// <summary>
        /// Whether the key ring can be shared through the database. It cannot without a connection string, nor during
        /// build-time OpenAPI document generation, where a configured development database is not reachable.
        /// </summary>
        internal static bool UseSharedKeyRing(string? connectionString, string? entryAssemblyName)
        {
            return !string.IsNullOrWhiteSpace(connectionString)
                && entryAssemblyName != OpenApiDocumentGeneratorAssembly;
        }

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
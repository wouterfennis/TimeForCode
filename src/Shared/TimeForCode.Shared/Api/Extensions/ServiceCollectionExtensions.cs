using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace TimeForCode.Shared.Api.Extensions
{
    /// <summary>
    /// Extension methods for configuring shared API services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds controllers with the default JSON serialisation options (enum values as strings).
        /// </summary>
        public static IMvcBuilder AddDefaultControllers(this IServiceCollection services)
        {
            return services.AddControllers()
                .AddJsonOptions(x =>
                {
                    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using OpenApiInfo = Microsoft.OpenApi.OpenApiInfo;

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

        /// <summary>
        /// Adds the project's default sliding-window rate-limiter settings to a policy.
        /// </summary>
        public static RateLimiterOptions AddDefaultSlidingWindowPolicy(this RateLimiterOptions options, string policyName, int permitLimit)
        {
            options.AddSlidingWindowLimiter(policyName, limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.SegmentsPerWindow = 6;
                limiterOptions.PermitLimit = permitLimit;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            return options;
        }

        /// <summary>
        /// Creates the default OpenAPI configuration for an API document.
        /// </summary>
        public static Action<OpenApiOptions> CreateDefaultOpenApiOptions(string title, string description)
        {
            return options =>
            {
                options.AddDocumentTransformer((openApiDocument, transformerContext, cancellationToken) =>
                {
                    openApiDocument.Info = new OpenApiInfo
                    {
                        Title = title,
                        Version = "v1",
                        Description = description
                    };

                    return Task.CompletedTask;
                });
            };
        }
    }
}
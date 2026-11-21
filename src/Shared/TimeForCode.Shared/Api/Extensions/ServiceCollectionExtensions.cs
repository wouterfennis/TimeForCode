using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
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
        private const int DefaultApiPermitLimit = 120;

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
        /// Reads the permit limit per minute of the "api" rate-limit policy (<c>RateLimiting:ApiPermitLimit</c>, default 120).
        /// </summary>
        public static int GetApiPermitLimit(this IConfiguration configuration)
        {
            int limit = configuration.GetValue("RateLimiting:ApiPermitLimit", DefaultApiPermitLimit);
            return limit > 0
                ? limit
                : throw new InvalidOperationException("RateLimiting:ApiPermitLimit must be greater than zero.");
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

            return RejectWithRetryAfter(options);
        }

        /// <summary>
        /// Like <see cref="AddDefaultSlidingWindowPolicy"/>, but every client (remote address, after the trusted forwarded headers) gets its own window,
        /// so one noisy client cannot use up the permits of all the others.
        /// </summary>
        public static RateLimiterOptions AddPerClientSlidingWindowPolicy(this RateLimiterOptions options, string policyName, int permitLimit)
        {
            options.AddPolicy(policyName, context => RateLimitPartition.GetSlidingWindowLimiter(
                GetClientPartitionKey(context),
                _ => new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    PermitLimit = permitLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

            return RejectWithRetryAfter(options);
        }

        /// <summary>
        /// The partition key of a request: the remote address, or one shared key when there is none.
        /// </summary>
        public static string GetClientPartitionKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private static RateLimiterOptions RejectWithRetryAfter(RateLimiterOptions options)
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                    context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

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

                options.AddOperationTransformer(OpenApiTransformers.AddRateLimitResponse);
                options.AddSchemaTransformer(OpenApiTransformers.AddEmailFormat);
                options.AddSchemaTransformer(OpenApiTransformers.AddStringEnumValues);
            };
        }
    }
}
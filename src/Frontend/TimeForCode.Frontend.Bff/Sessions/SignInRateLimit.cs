using Microsoft.Extensions.Options;
using System.Globalization;
using System.Threading.RateLimiting;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// A request limit per client address for the endpoints an attacker could hammer: the sign-in start and callback, and the admin
    /// passkey ceremonies (which take the bootstrap secret). One client cannot lock the others out, unlike a single shared bucket.
    /// </summary>
    internal static class SignInRateLimit
    {
        public const string PolicyName = "bff-sign-in";

        private const int SegmentsPerWindow = 6;
        private const int SegmentSeconds = 60 / SegmentsPerWindow;

        public static IServiceCollection AddSignInRateLimit(this IServiceCollection services) =>
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = (context, _) =>
                {
                    // A sliding window gives no hint, so the answer is how long a segment takes to free (window / segments).
                    var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? (int)Math.Ceiling(retryAfter.TotalSeconds) : SegmentSeconds;
                    context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                    return ValueTask.CompletedTask;
                };
                options.AddPolicy(PolicyName, context =>
                {
                    var limit = context.RequestServices.GetRequiredService<IOptions<BrowserSessionOptions>>().Value.SignInRequestsPerMinute;
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = SegmentsPerWindow,
                            PermitLimit = limit,
                            QueueLimit = 0
                        });
                });
            });
    }
}
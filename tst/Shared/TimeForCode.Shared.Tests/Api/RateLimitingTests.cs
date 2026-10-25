using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Shared.Tests.Api
{
    [TestClass]
    public class RateLimitingTests
    {
        [TestMethod]
        public async Task OnRejected_LeaseWithRetryAfter_WritesRetryAfterHeaderInWholeSeconds()
        {
            var options = new RateLimiterOptions().AddDefaultSlidingWindowPolicy("api", 60);
            var context = BuildContext(new RetryAfterLease(TimeSpan.FromSeconds(11.2)));

            await options.OnRejected!(context, CancellationToken.None);

            context.HttpContext.Response.Headers.RetryAfter.ToString().Should().Be("12");
        }

        [TestMethod]
        public async Task OnRejected_LeaseWithoutRetryAfter_DoesNotWriteRetryAfterHeader()
        {
            var options = new RateLimiterOptions().AddDefaultSlidingWindowPolicy("api", 60);
            var context = BuildContext(new RetryAfterLease(null));

            await options.OnRejected!(context, CancellationToken.None);

            context.HttpContext.Response.Headers.ContainsKey("Retry-After").Should().BeFalse();
        }

        [TestMethod]
        public void AddDefaultSlidingWindowPolicy_RejectsWithTooManyRequests()
        {
            var options = new RateLimiterOptions().AddDefaultSlidingWindowPolicy("api", 60);

            options.RejectionStatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        }

        private static OnRejectedContext BuildContext(RateLimitLease lease)
        {
            return new OnRejectedContext { HttpContext = new DefaultHttpContext(), Lease = lease };
        }

        private sealed class RetryAfterLease(TimeSpan? retryAfter) : RateLimitLease
        {
            public override bool IsAcquired => false;

            public override IEnumerable<string> MetadataNames => retryAfter.HasValue ? [MetadataName.RetryAfter.Name] : [];

            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                metadata = metadataName == MetadataName.RetryAfter.Name ? retryAfter : null;
                return metadata != null;
            }
        }
    }
}
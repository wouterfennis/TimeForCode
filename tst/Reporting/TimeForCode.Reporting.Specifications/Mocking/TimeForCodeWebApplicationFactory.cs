using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using TimeForCode.Reporting.Api;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Reporting.Specifications.Mocking
{
    public class TimeForCodeWebApplicationFactory : WebApplicationFactory<Startup>
    {
        private static readonly string[] LimitedPolicies = ["public", "badge"];

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDonationDataSource>();

                var donationDataSource = new InMemoryDonationDataSource();
                services.TryAddSingleton(donationDataSource);
                services.TryAddSingleton<IDonationDataSource>(donationDataSource);

                services.RemoveAll<IBadgeTierRepository>();
                services.RemoveAll<IBadgeAwardRepository>();
                var tierRepository = new InMemoryBadgeTierRepository();
                var awardRepository = new InMemoryBadgeAwardRepository();
                services.AddSingleton(tierRepository);
                services.AddSingleton(awardRepository);
                services.AddSingleton<IBadgeTierRepository>(tierRepository);
                services.AddSingleton<IBadgeAwardRepository>(awardRepository);

                // One host serves every scenario of a run, so the production limit of 60 requests a minute would reject later scenarios.
                services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
                var publicRateLimit = new PublicRateLimit();
                services.AddSingleton(publicRateLimit);
                services.Configure<RateLimiterOptions>(options =>
                {
                    options.AddDefaultSlidingWindowPolicy("api", int.MaxValue);

                    // Unlimited, until a scenario lowers it; the partition key changes with every reset, so each scenario starts with a fresh window.
                    // The "badge" policy of the badge images follows the same limit.
                    foreach (string policy in LimitedPolicies)
                    {
                        options.AddPolicy(policy, _ => RateLimitPartition.GetSlidingWindowLimiter(
                            $"{policy}-{publicRateLimit.Window}",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 6,
                                PermitLimit = publicRateLimit.PermitLimit,
                                QueueLimit = 0
                            }));
                    }
                });

                // Override JWT validation to accept symmetric test tokens
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    // The host runs in Development, where Authority points at a stack that is not running; loading its metadata fails slowly on every authenticated request.
                    options.Authority = null;
                    // MetadataAddress is declared non-nullable, but null is the only value that stops the handler from deriving a metadata address.
                    options.MetadataAddress = null!;
                    options.ConfigurationManager = null;
                    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.TestJwtSigningKey));
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey
                    };
                });
            });
        }

        /// <summary>Returns the shared host to its initial state, so one host can serve every scenario of a test run.</summary>
        public void ResetState()
        {
            Services.GetRequiredService<InMemoryDonationDataSource>().Clear();
            Services.GetRequiredService<InMemoryBadgeTierRepository>().Clear();
            Services.GetRequiredService<InMemoryBadgeAwardRepository>().Clear();
            Services.GetRequiredService<PublicRateLimit>().Reset();
        }
    }
}
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TimeForCode.Reporting.Api;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Reporting.Specifications.Mocking
{
    public class TimeForCodeWebApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDonationDataSource>();

                var donationDataSource = new InMemoryDonationDataSource();
                services.TryAddSingleton(donationDataSource);
                services.TryAddSingleton<IDonationDataSource>(donationDataSource);

                // One host serves every scenario of a run, so the production limit of 60 requests a minute would reject later scenarios.
                services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
                services.Configure<RateLimiterOptions>(options => options.AddDefaultSlidingWindowPolicy("api", int.MaxValue));

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
        }
    }
}
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Api.Options;
using TimeForCode.Donation.Application.Extensions;
using TimeForCode.Donation.Infrastructure.Extensions;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Donation.Api
{
    /// <summary>
    /// Startup class.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Application startup wiring")]
    public class Startup
    {
        private IConfiguration _configuration { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenApi(
                "v1",
                TimeForCode.Shared.Api.Extensions.ServiceCollectionExtensions.CreateDefaultOpenApiOptions(
                    "Donation API",
                    "API to interact with the donation backend"));
            services.AddDefaultControllers();

            var authenticationOptions = AuthenticationOptions.Bind(_configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authenticationOptions.Authority;
                    options.RequireHttpsMetadata = authenticationOptions.RequireHttpsMetadata;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = authenticationOptions.Issuer,
                        ValidAudience = authenticationOptions.Audience
                    };
                });

            services.AddAuthorizationBuilder()
                    .AddPolicy("ApiUser", policy => policy.RequireClaim("scope", "user"));

            services.AddApplicationLayer();
            services.AddInfrastructureLayer(_configuration);
            services.AddRateLimiter(options => options.AddDefaultSlidingWindowPolicy("api", 60));
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDefaultApiPipeline(env);
        }
    }
}
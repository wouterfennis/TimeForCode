using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Authorization.Application.Extensions;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Infrastructure.Extensions;
using TimeForCode.Shared.Api.Extensions;
using SharedApiServiceCollectionExtensions = TimeForCode.Shared.Api.Extensions.ServiceCollectionExtensions;

namespace TimeForCode.Authorization.Api
{
    /// <summary>
    /// Startup class.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Application startup wiring")]
    public class Startup
    {
        private IConfiguration _configuration { get; }
        private IWebHostEnvironment _environment { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            TestAdminAccessOptions.Bind(_configuration).EnsureSafeFor(_environment.EnvironmentName);

            var authenticationOptions = AuthenticationOptions.Bind(_configuration);

            services.AddCors(options =>
            {
                options.AddPolicy("AdminCorsPolicy", policy =>
                {
                    // The admin flow's fetch calls use credentials: "include" (HttpOnly cookie round-trip),
                    // so the allowed origins must be explicit (no AllowAnyOrigin) and AllowCredentials is required.
                    policy.WithOrigins(authenticationOptions.ValidRedirectUris.ToArray())
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            services.AddOpenApi(
                "v1",
                SharedApiServiceCollectionExtensions.CreateDefaultOpenApiOptions(
                    "Authorization API",
                    "API to interact with the authorization backend"));
            services.AddDefaultControllers();

            services.AddApplicationLayer(_configuration, allowEphemeralSigningKey: _environment.IsDevelopment());
            services.AddInfrastructureLayer(_configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
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

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<SigningKeySet>((options, signingKeys) =>
                    options.TokenValidationParameters.IssuerSigningKeys = signingKeys.PublishedKeys);

            services.AddAuthorizationBuilder()
                    .AddPolicy("ApiUser", policy => policy.RequireClaim("scope", "user"));

            services.AddRateLimiter(options =>
            {
                options.AddDefaultSlidingWindowPolicy("auth", 20);
                options.AddDefaultSlidingWindowPolicy("admin-auth", 20);
            });
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using TimeForCode.Authorization.Application.Extensions;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Infrastructure.Extensions;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Authorization.Api
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
            services.AddOpenApi("v1", options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "Authorization API",
                        Version = "v1",
                        Description = "API to interact with the authorization backend"
                    };
                    return Task.CompletedTask;
                });
            });

            services.AddDefaultControllers();

            services.AddApplicationLayer(_configuration);
            services.AddInfrastructureLayer(_configuration);

            var authenticationOptions = AuthenticationOptions.Bind(_configuration);
            var rsa = Application.Extensions.ServiceCollectionExtensions.LoadCertificate(_configuration);

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
                        ValidAudience = authenticationOptions.Audience,
                        IssuerSigningKey = new RsaSecurityKey(rsa)
                    };
                });

            services.AddAuthorizationBuilder()
                    .AddPolicy("ApiUser", policy => policy.RequireClaim("scope", "user"));

            services.AddRateLimiter(options =>
            {
                options.AddSlidingWindowLimiter("auth", opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.SegmentsPerWindow = 6;
                    opt.PermitLimit = 20;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
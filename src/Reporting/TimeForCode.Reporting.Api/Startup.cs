using System.Diagnostics.CodeAnalysis;
using TimeForCode.Reporting.Application.Extensions;
using TimeForCode.Reporting.Infrastructure.Extensions;
using TimeForCode.Shared.Api.Extensions;
using SharedApiServiceCollectionExtensions = TimeForCode.Shared.Api.Extensions.ServiceCollectionExtensions;

namespace TimeForCode.Reporting.Api
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
                SharedApiServiceCollectionExtensions.CreateDefaultOpenApiOptions(
                    "Reporting API",
                    "API for the impact report and contribution history export of donor organizations"));
            services.AddDefaultControllers();

            services.AddJwtBearerAuthentication(_configuration);

            services.AddAuthorizationBuilder()
                    .AddPolicy("ApiUserOrAdmin", policy => policy.RequireClaim(AuthenticationExtensions.ScopeClaim, "user", "admin"));

            services.AddApplicationLayer();
            services.AddInfrastructureLayer(_configuration);
            services.AddRateLimiter(options => options.AddDefaultSlidingWindowPolicy("api", _configuration.GetApiPermitLimit()));
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
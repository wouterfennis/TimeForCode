using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Frontend.Bff.Sessions;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// Starting point of the BFF.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Application entrypoint")]
    public class Program
    {
        // Only a type to hang the entry point on (and to name in the web application factory of the tests); nobody creates one.
        protected Program()
        {
        }

        /// <summary>
        /// Starting point of the BFF.
        /// </summary>
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            Action<OpenApiOptions> defaultOpenApiOptions = TimeForCode.Shared.Api.Extensions.ServiceCollectionExtensions.CreateDefaultOpenApiOptions(
                "Front-end BFF",
                "The browser-facing sign-in, session status and sign-out. The BFF keeps the tokens; the browser only holds one opaque session cookie.");
            builder.Services.AddOpenApi("v1", options =>
            {
                defaultOpenApiOptions(options);
                BffOpenApi.Describe(options);
            });
            builder.Services.AddTrustedForwardedHeaders(builder.Configuration);
            builder.Services.AddSignInRateLimit();
            builder.Services.AddExceptionHandler<UpstreamFailureHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.Configure<BrowserSessionOptions>(builder.Configuration.GetSection(BrowserSessionOptions.SectionName));
            builder.Services.Configure<AuthorizationOptions>(builder.Configuration.GetSection(AuthorizationOptions.SectionName));
            builder.Services.Configure<DbOptions>(builder.Configuration.GetSection(DbOptions.SectionName));
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ISessionStore, MongoSessionStore>();
            builder.Services.AddSingleton<SessionService>();
            // The address is read when the client is first used, not at start: the OpenAPI build step starts the host without configuration.
            builder.Services.AddHttpClient<IAuthorizationApi, AuthorizationApi>((provider, client) =>
            {
                client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value.BaseAddress);
                // Well below the 30 s refresh lease, so a slow refresh ends before another request can take the lease over.
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                .AddTransforms(LocationRewrite.Add)
                .AddTransforms(SessionBearer.Add);

            WebApplication app = builder.Build();

            // First, so the scheme and host that HSTS and the Secure cookies see are the original ones behind a proxy.
            app.UseForwardedHeaders();
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseExceptionHandler();
            app.UseBrowserSecurityHeaders();
            app.UseAdminBodyLimit();
            app.UseRateLimiter();
            app.UseCsrfHeaderCheck();
            app.UseAuthRouteAllowlist();
            app.MapBffEndpoints();
            app.MapReverseProxy();
            app.MapSpaFiles();

            await app.RunAsync();
        }
    }
}
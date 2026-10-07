using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Donation.Api.Client.Extensions;
using TimeForCode.Website.Components;
using TimeForCode.Website.Components.Authentication;
using TimeForCode.Website.Options;

namespace TimeForCode.Website
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Application entrypoint")]
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var storageOptions = StorageOptions.Bind(builder.Configuration);
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(storageOptions.StoragePath))
                .SetApplicationName("TimeForCode.Website");

            builder.Services.AddHttpContextAccessor();

            // AuthorizeRouteView requires IAuthenticationService and a default scheme to be registered,
            // even though the actual identity comes from JwtCookieAuthenticationStateProvider — this
            // cookie scheme is never signed into. Blazor's static-SSR authorization fallback challenges
            // via this scheme's LoginPath/AccessDeniedPath when a page's [Authorize] fails, so both are
            // pointed at the home page rather than the (nonexistent) default "/Account/Login".
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/";
                    options.AccessDeniedPath = "/";
                });
            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<AuthenticationStateProvider, JwtCookieAuthenticationStateProvider>();

            builder.Services
                .Configure<AuthorizationServiceOptions>(options => builder.Configuration.GetSection(AuthorizationServiceOptions.SectionName)
                .Bind(options));

            builder.Services
                .Configure<DonationServiceOptions>(options => builder.Configuration.GetSection(DonationServiceOptions.SectionName)
                .Bind(options));

            var authorizationServiceOptions = AuthorizationServiceOptions.Bind(builder.Configuration);
            var donationServiceOptions = DonationServiceOptions.Bind(builder.Configuration);
            builder.Services.AddAuthClient(new Uri(authorizationServiceOptions.BaseUri));
            builder.Services.AddDonationClient(new Uri(donationServiceOptions.BaseUri));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();
            // Intentionally no UseAuthentication()/UseAuthorization() middleware: [Authorize] on our pages
            // is enforced entirely by Blazor's own AuthorizeRouteView, which renders the NotAuthorized
            // fragment inline. The HTTP-level middleware would instead challenge via the cookie scheme's
            // default login path (which doesn't exist here) before AuthorizeRouteView ever gets to render.
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
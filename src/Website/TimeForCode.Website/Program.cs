using Microsoft.AspNetCore.DataProtection;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Website.Components;
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

            var authorizationServiceOptions = AuthorizationServiceOptions.Bind(builder.Configuration);
            builder.Services.AddAuthClient(new Uri(authorizationServiceOptions.BaseUri));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
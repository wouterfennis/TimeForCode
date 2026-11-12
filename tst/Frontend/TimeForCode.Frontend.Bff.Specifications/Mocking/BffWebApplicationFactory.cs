using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace TimeForCode.Frontend.Bff.Specifications.Mocking
{
    /// <summary>
    /// Starts the BFF in memory. Access tokens are signed with <see cref="SigningKey"/> instead of the Authorization API's
    /// published keys, so no running stack is needed; issuer, audience and lifetime are still validated as configured.
    /// </summary>
    public sealed class BffWebApplicationFactory : WebApplicationFactory<Program>
    {
        public const string Issuer = "http://authorization-api:8080";
        public const string Audience = "http://authorization-api:8080";

        public static SymmetricSecurityKey SigningKey { get; } =
            new(Encoding.UTF8.GetBytes("bff-specifications-signing-key-of-at-least-32-bytes"));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("AuthenticationOptions:Authority", Issuer);
            builder.UseSetting("AuthenticationOptions:Issuer", Issuer);
            builder.UseSetting("AuthenticationOptions:Audience", Audience);
            builder.ConfigureTestServices(services =>
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    // Without these the handler loads the Authority's metadata from a stack that is not running.
                    options.Authority = null;
                    options.MetadataAddress = null!;
                    options.ConfigurationManager = null;
                    options.TokenValidationParameters.IssuerSigningKey = SigningKey;
                }));
        }

        public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
    }
}
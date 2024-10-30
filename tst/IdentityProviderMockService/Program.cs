
using IdentityProviderMockService.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace IdentityProviderMockService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddMemoryCache();
            builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
            builder.Services.Configure<RsaOptions>(builder.Configuration.GetSection(RsaOptions.SectionName));

            // Bind AuthenticationOptions
            var authenticationOptions = new AuthenticationOptions();
            builder.Configuration.GetSection(AuthenticationOptions.SectionName).Bind(authenticationOptions);

            // Bind RsaOptions
            var rsaOptions = new RsaOptions();
            builder.Configuration.GetSection(RsaOptions.SectionName).Bind(rsaOptions);

            // Convert Base64 string to byte array
            var certificateBytes = Convert.FromBase64String(rsaOptions.Base64Certificate);

            // Load the certificate
            var certificate = new X509Certificate2(certificateBytes, (string?)null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            var rsa = certificate.GetRSAPrivateKey()!;

            builder.Services.AddSingleton(rsa);

            // Add Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

            // Add Authorization
            builder.Services.AddAuthorizationBuilder()
                            .AddPolicy("ApiUser", policy => policy.RequireClaim("scope", "user"));

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity provider mock API v1");
                c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
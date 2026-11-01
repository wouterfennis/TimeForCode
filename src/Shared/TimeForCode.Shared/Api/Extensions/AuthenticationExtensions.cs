using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Shared.Api.Extensions
{
    /// <summary>
    /// Extension methods for the bearer-token authentication shared by the resource APIs.
    /// </summary>
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// The claim carrying the scope of the caller.
        /// </summary>
        public const string ScopeClaim = "scope";

        /// <summary>
        /// Adds JWT bearer authentication configured from the <see cref="JwtAuthenticationOptions"/> section.
        /// </summary>
        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var authenticationOptions = JwtAuthenticationOptions.Bind(configuration);

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

            return services;
        }
    }
}
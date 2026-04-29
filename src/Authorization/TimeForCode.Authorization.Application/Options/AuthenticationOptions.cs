using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Application.Options
{
    /// <summary>
    /// Options for authentication
    /// </summary>
    public class AuthenticationOptions
    {
        public const string SectionName = "AuthenticationOptions";

        /// <summary>
        /// Token issuer
        /// </summary>
        [Required]
        public required string Issuer { get; init; }

        /// <summary>
        /// Token audience
        /// </summary>
        [Required]
        public required string Audience { get; init; }

        /// <summary>
        /// Valid redirect uris
        /// </summary>
        [Required]
        public required List<string> ValidRedirectUris { get; init; }

        /// <summary>
        /// Bind the configuration to the AuthenticationOptions
        /// </summary>
        public static AuthenticationOptions Bind(IConfiguration configuration)
        {
            var authenticationOptions = new AuthenticationOptions
            {
                Audience = configuration.GetSection(SectionName).GetValue<string>(nameof(Audience))!,
                Issuer = configuration.GetSection(SectionName).GetValue<string>(nameof(Issuer))!,
                ValidRedirectUris = configuration.GetSection(SectionName).GetValue<List<string>>(nameof(ValidRedirectUris))!,
            };

            return authenticationOptions;
        }
    }
}
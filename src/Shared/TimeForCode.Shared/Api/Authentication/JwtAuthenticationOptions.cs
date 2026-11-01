using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Shared.Api.Authentication
{
    /// <summary>
    /// Options for validating the bearer tokens issued by the Authorization service.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class JwtAuthenticationOptions
    {
        /// <summary>
        /// Name of the configuration section for authentication options
        /// </summary>
        public const string SectionName = "AuthenticationOptions";

        /// <summary>
        /// Token authority (metadata endpoint base)
        /// </summary>
        [Required]
        public required string Authority { get; init; }

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
        /// Whether the metadata endpoint must use HTTPS
        /// </summary>
        public bool RequireHttpsMetadata { get; init; } = true;

        /// <summary>
        /// Bind the configuration to the options
        /// </summary>
        public static JwtAuthenticationOptions Bind(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName);
            return new JwtAuthenticationOptions
            {
                Authority = section.GetValue<string>(nameof(Authority))!,
                Audience = section.GetValue<string>(nameof(Audience))!,
                Issuer = section.GetValue<string>(nameof(Issuer))!,
                RequireHttpsMetadata = section.GetValue(nameof(RequireHttpsMetadata), true),
            };
        }
    }
}
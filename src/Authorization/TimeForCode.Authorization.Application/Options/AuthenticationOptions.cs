using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Authorization.Application.Options
{
    /// <summary>
    /// Options for authentication
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
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
        /// How long a single-use login code can be exchanged for tokens, in seconds. Defaults to 60.
        /// </summary>
        public int LoginCodeLifetimeInSeconds { get; init; } = DefaultLoginCodeLifetimeInSeconds;

        public const int DefaultLoginCodeLifetimeInSeconds = 60;

        /// <summary>
        /// Bind the configuration to the AuthenticationOptions
        /// </summary>
        public static AuthenticationOptions Bind(IConfiguration configuration)
        {
            var authenticationOptions = new AuthenticationOptions
            {
                Audience = configuration.GetSection(SectionName).GetValue<string>(nameof(Audience))!,
                Issuer = configuration.GetSection(SectionName).GetValue<string>(nameof(Issuer))!,
                ValidRedirectUris = configuration.GetSection($"{SectionName}:{nameof(ValidRedirectUris)}").Get<List<string>>() ?? [],
                LoginCodeLifetimeInSeconds = configuration.GetSection(SectionName).GetValue<int?>(nameof(LoginCodeLifetimeInSeconds)) ?? DefaultLoginCodeLifetimeInSeconds,
            };

            return authenticationOptions;
        }
    }
}
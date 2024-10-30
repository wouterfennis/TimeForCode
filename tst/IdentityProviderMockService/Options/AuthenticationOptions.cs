using System.ComponentModel.DataAnnotations;

namespace IdentityProviderMockService.Options
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
        /// Token expiration time in minutes
        /// </summary>
        [Required]
        public required int ExpiresInMinutes { get; init; }

        /// <summary>
        /// Expected client id for returning a token
        /// </summary>
        [Required]
        public required string ExpectedClientId { get; init; }

        /// <summary>
        /// Expected client secret for returning a token
        /// </summary>
        [Required]
        public required string ExpectedClientSecret { get; init; }

        public static AuthenticationOptions Bind(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var authenticationOptions = new AuthenticationOptions
            {
                Audience = configuration.GetSection(SectionName).GetValue<string>(nameof(Audience))!,
                Issuer = configuration.GetSection(SectionName).GetValue<string>(nameof(Issuer))!,
                ExpectedClientId = configuration.GetSection(SectionName).GetValue<string>(nameof(ExpectedClientId))!,
                ExpectedClientSecret = configuration.GetSection(SectionName).GetValue<string>(nameof(ExpectedClientSecret))!,
                ExpiresInMinutes = configuration.GetSection(SectionName).GetValue<int>(nameof(ExpiresInMinutes)),
            };
            return authenticationOptions;
        }
    }
}
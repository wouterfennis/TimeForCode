using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Donation.Api.Options
{
    /// <summary>
    /// Options for authentication
    /// </summary>
    public class AuthenticationOptions
    {
        /// <summary>
        /// Name of the configuration section for authentication options
        /// </summary>
        public const string SectionName = "AuthenticationOptions";

        /// <summary>
        /// Token issuer
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
        /// Bind the configuration to the AuthenticationOptions
        /// </summary>
        public static AuthenticationOptions Bind(IConfiguration configuration)
        {
            var authenticationOptions = new AuthenticationOptions
            {
                Authority = configuration.GetSection(SectionName).GetValue<string>(nameof(Authority))!,
                Audience = configuration.GetSection(SectionName).GetValue<string>(nameof(Audience))!,
                Issuer = configuration.GetSection(SectionName).GetValue<string>(nameof(Issuer))!,
            };

            return authenticationOptions;
        }
    }
}
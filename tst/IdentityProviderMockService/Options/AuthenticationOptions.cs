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
        public required string Issuer { get; set; }

        /// <summary>
        /// Token audience
        /// </summary>
        [Required]
        public required string Audience { get; set; }

        /// <summary>
        /// Token expiration time in minutes
        /// </summary>
        [Required]
        public required int ExpiresInMinutes { get; set; }

        /// <summary>
        /// Expected client id for returning a token
        /// </summary>
        [Required]
        public required string ExpectedClientId { get; set; }

        /// <summary>
        /// Expected client secret for returning a token
        /// </summary>
        [Required]
        public required string ExpectedClientSecret { get; set; }
    }
}

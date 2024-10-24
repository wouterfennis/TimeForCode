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
        public string Issuer { get; set; }

        /// <summary>
        /// Token audience
        /// </summary>
        [Required]
        public string Audience { get; set; }

        /// <summary>
        /// Token expiration time in minutes
        /// </summary>
        [Required]
        public int ExpiresInMinutes { get; set; }

        /// <summary>
        /// Expected client id for returning a token
        /// </summary>
        [Required]
        public string ExpectedClientId { get; set; }

        /// <summary>
        /// Expected client secret for returning a token
        /// </summary>
        [Required]
        public string ExpectedClientSecret { get; set; }
    }
}

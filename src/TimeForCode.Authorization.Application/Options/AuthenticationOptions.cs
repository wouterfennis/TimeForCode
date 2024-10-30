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
        public int TokenExpiresInMinutes { get; set; }

        /// <summary>
        /// Default refresh token expiration time in days
        /// </summary>
        [Required]
        public required int DefaultRefreshTokenExpirationAfterInDays { get; init; }
    }
}

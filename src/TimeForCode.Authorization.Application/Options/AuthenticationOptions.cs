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
        /// Token expiration time in minutes
        /// </summary>
        [Required]
        public required int TokenExpiresInMinutes { get; init; }

        /// <summary>
        /// Default refresh token expiration time in days
        /// </summary>
        [Required]
        public required int DefaultRefreshTokenExpirationAfterInDays { get; init; }
    }
}
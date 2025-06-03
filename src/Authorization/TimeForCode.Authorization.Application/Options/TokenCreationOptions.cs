using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Application.Options
{
    /// <summary>
    /// Options for token creation
    /// </summary>
    public class TokenCreationOptions
    {
        public const string SectionName = "TokenCreationOptions";

        /// <summary>
        /// Token issuer
        /// </summary>
        [Required]
        public required string Issuer { get; init; }

        /// <summary>
        /// Token audience
        /// </summary>
        [Required]
        public required List<string> Audiences { get; init; }

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

        public static TokenCreationOptions Bind(IConfiguration configuration)
        {
            var tokenCreationOptions = new TokenCreationOptions
            {
                Audiences = configuration.GetSection(SectionName).GetValue<List<string>>(nameof(Audiences))!,
                Issuer = configuration.GetSection(SectionName).GetValue<string>(nameof(Issuer))!,
                TokenExpiresInMinutes = configuration.GetSection(SectionName).GetValue<int>(nameof(TokenExpiresInMinutes)),
                DefaultRefreshTokenExpirationAfterInDays = configuration.GetSection(SectionName).GetValue<int>(nameof(DefaultRefreshTokenExpirationAfterInDays)),
            };

            return tokenCreationOptions;
        }
    }
}
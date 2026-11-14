using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Authorization.Application.Options
{
    /// <summary>
    /// Options for token creation
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
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

        /// <summary>
        /// The longest access token lifetime in minutes. An issued access token cannot be revoked, so its lifetime is the exposure window.
        /// </summary>
        public const int MaximumTokenExpiresInMinutes = 15;

        /// <summary>
        /// Fails closed: throws when the access token lifetime is not between 1 and <see cref="MaximumTokenExpiresInMinutes"/> minutes,
        /// so a missing value (0) or a long-lived token never reaches production by default.
        /// </summary>
        public void EnsureValidLifetime()
        {
            if (TokenExpiresInMinutes is < 1 or > MaximumTokenExpiresInMinutes)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:{nameof(TokenExpiresInMinutes)} must be between 1 and {MaximumTokenExpiresInMinutes} minutes, but is {TokenExpiresInMinutes}.");
            }
        }

        public static TokenCreationOptions Bind(IConfiguration configuration)
        {
            var tokenCreationOptions = new TokenCreationOptions
            {
                Audiences = configuration.GetSection($"{SectionName}:{nameof(Audiences)}").Get<List<string>>()!,
                Issuer = configuration.GetSection(SectionName).GetValue<string>(nameof(Issuer))!,
                TokenExpiresInMinutes = configuration.GetSection(SectionName).GetValue<int>(nameof(TokenExpiresInMinutes)),
                DefaultRefreshTokenExpirationAfterInDays = configuration.GetSection(SectionName).GetValue<int>(nameof(DefaultRefreshTokenExpirationAfterInDays)),
            };

            return tokenCreationOptions;
        }
    }
}
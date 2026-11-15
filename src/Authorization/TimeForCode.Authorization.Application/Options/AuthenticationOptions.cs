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
        /// The refresh cookie path when clients call the API on its own path, without a proxy or gateway that publishes it under another prefix.
        /// </summary>
        public const string DefaultRefreshCookiePath = "/api/v1/Authentication/session";

        /// <summary>
        /// The <c>Path</c> of the refresh token cookie: the public path the browser uses for the session endpoints
        /// (refresh and logout), so the browser sends the cookie to those two endpoints and to no other route.
        /// When a proxy or gateway publishes the API under another prefix, this is that public path, not the API's own path. Browsers match it case-sensitively.
        /// </summary>
        public string RefreshCookiePath { get; init; } = DefaultRefreshCookiePath;

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
                RefreshCookiePath = configuration.GetSection(SectionName).GetValue<string>(nameof(RefreshCookiePath)) ?? DefaultRefreshCookiePath,
            };

            return authenticationOptions;
        }
    }
}
using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Website.Options
{
    public class AuthorizationServiceOptions
    {
        public const string SectionName = "AuthorizationServiceOptions";

        [Required]
        public required string BaseUri { get; init; }

        /// <summary>
        /// The publicly accessible base URI of the authorization API, used for browser-facing links.
        /// Defaults to <see cref="BaseUri"/> when not set.
        /// </summary>
        public string? PublicBaseUri { get; init; }

        /// <summary>
        /// Returns <see cref="PublicBaseUri"/> if set, otherwise falls back to <see cref="BaseUri"/>.
        /// </summary>
        public string EffectivePublicBaseUri => PublicBaseUri ?? BaseUri;

        public static AuthorizationServiceOptions Bind(IConfiguration configuration)
        {
            var authenticationOptions = new AuthorizationServiceOptions
            {
                BaseUri = configuration.GetSection(SectionName).GetValue<string>(nameof(BaseUri))!,
                PublicBaseUri = configuration.GetSection(SectionName).GetValue<string>(nameof(PublicBaseUri)),
            };

            return authenticationOptions;
        }
    }
}
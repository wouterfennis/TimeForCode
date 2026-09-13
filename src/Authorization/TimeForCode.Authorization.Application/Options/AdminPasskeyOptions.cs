using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Authorization.Application.Options
{
    /// <summary>
    /// Options for the admin WebAuthn passkey flow. Kept separate from <see cref="AuthenticationOptions"/>/
    /// <see cref="ExternalIdentityProviderOptions"/> so the admin slice stays independently configurable.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class AdminPasskeyOptions
    {
        public const string SectionName = "AdminPasskeyOptions";

        /// <summary>
        /// The relying-party (origin) domain the browser calls <c>navigator.credentials</c> from — the
        /// Website's origin, not the Authorization API's own host. Must be explicit; never inferred from
        /// the request host header.
        /// </summary>
        [Required]
        public required string ServerDomain { get; init; }

        /// <summary>
        /// Display name of the relying party shown by the browser/authenticator during the ceremony.
        /// </summary>
        [Required]
        public required string RelyingPartyName { get; init; }

        /// <summary>
        /// Shared secret that must be supplied to claim the admin role. Acts as a race-condition guard
        /// alongside the atomic "insert-if-none-exists" credential write.
        /// </summary>
        [Required]
        public required string BootstrapSecret { get; init; }

        public static AdminPasskeyOptions Bind(IConfiguration configuration)
        {
            return new AdminPasskeyOptions
            {
                ServerDomain = configuration.GetSection(SectionName).GetValue<string>(nameof(ServerDomain))!,
                RelyingPartyName = configuration.GetSection(SectionName).GetValue<string>(nameof(RelyingPartyName))!,
                BootstrapSecret = configuration.GetSection(SectionName).GetValue<string>(nameof(BootstrapSecret))!,
            };
        }
    }
}
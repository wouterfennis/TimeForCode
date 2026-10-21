using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Authorization.Application.Options
{
    /// <summary>
    /// Options for the test-only admin token endpoint that lets live tests act as the admin without a WebAuthn
    /// passkey. Disabled by default; <see cref="EnsureSafeFor"/> refuses to start it outside local and test environments.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class TestAdminAccessOptions
    {
        public const string SectionName = "TestAdminAccess";

        public const int MinimumSecretLength = 16;

        private static readonly string[] AllowedEnvironments = ["Development", "Test"];

        /// <summary>
        /// Whether the test-only admin token endpoint is active. Defaults to <c>false</c>.
        /// </summary>
        public bool Enabled { get; init; }

        /// <summary>
        /// Shared secret a caller must present to obtain the admin token.
        /// </summary>
        public string Secret { get; init; } = string.Empty;

        public static TestAdminAccessOptions Bind(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName);
            return new TestAdminAccessOptions
            {
                Enabled = section.GetValue<bool>(nameof(Enabled)),
                Secret = section.GetValue<string>(nameof(Secret)) ?? string.Empty
            };
        }

        /// <summary>
        /// Fails closed: throws when the endpoint is enabled in any environment other than Development or Test,
        /// or without a sufficiently long secret.
        /// </summary>
        public void EnsureSafeFor(string environmentName)
        {
            if (!Enabled)
            {
                return;
            }

            if (!AllowedEnvironments.Contains(environmentName, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{SectionName}:{nameof(Enabled)} must not be true in the '{environmentName}' environment; it is allowed in Development and Test only.");
            }

            if (Secret.Length < MinimumSecretLength)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:{nameof(Secret)} must be at least {MinimumSecretLength} characters when {SectionName} is enabled.");
            }
        }
    }
}
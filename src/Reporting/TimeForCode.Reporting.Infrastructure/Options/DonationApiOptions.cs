using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Reporting.Infrastructure.Options
{
    /// <summary>
    /// Options for the connection to the Donation API.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class DonationApiOptions
    {
        /// <summary>
        /// Name of the configuration section for the Donation API options.
        /// </summary>
        public const string SectionName = "DonationApiOptions";

        /// <summary>
        /// Base address of the Donation API, for example <c>http://donation-api:8080</c>.
        /// </summary>
        public string? BaseUri { get; set; }
    }
}
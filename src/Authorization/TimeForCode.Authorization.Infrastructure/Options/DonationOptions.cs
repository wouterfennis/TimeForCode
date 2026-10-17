using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Authorization.Infrastructure.Options
{
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class DonationOptions
    {
        public const string SectionName = "DonationOptions";

        /// <summary>
        /// Base address of the donation API the authorization API calls for account deletion.
        /// </summary>
        [Required]
        public required string BaseUri { get; init; }
    }
}
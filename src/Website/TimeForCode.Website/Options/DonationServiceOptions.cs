using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Website.Options
{
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class DonationServiceOptions
    {
        public const string SectionName = "DonationServiceOptions";

        [Required]
        public required string BaseUri { get; init; }

        public static DonationServiceOptions Bind(IConfiguration configuration)
        {
            var donationServiceOptions = new DonationServiceOptions
            {
                BaseUri = configuration.GetSection(SectionName).GetValue<string>(nameof(BaseUri))!,
            };

            return donationServiceOptions;
        }
    }
}
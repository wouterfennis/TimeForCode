using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Donation.Infrastructure.Options
{
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class GithubApiOptions
    {
        public const string SectionName = "GithubApiOptions";

        [Required]
        public required string BaseUrl { get; init; }
    }
}
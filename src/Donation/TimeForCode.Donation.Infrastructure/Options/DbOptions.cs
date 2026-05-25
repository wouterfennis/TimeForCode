using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Donation.Infrastructure.Options
{
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class DbOptions
    {
        public const string SectionName = "DbOptions";

        [Required]
        public required string ConnectionString { get; init; }

        [Required]
        public required string DatabaseName { get; init; }
    }
}
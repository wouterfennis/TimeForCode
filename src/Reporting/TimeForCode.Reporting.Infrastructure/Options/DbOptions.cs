using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Reporting.Infrastructure.Options
{
    /// <summary>
    /// Options for the MongoDB database that holds the badge tiers and awards. Reporting owns this database; it does not share one with another context.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class DbOptions
    {
        /// <summary>
        /// Name of the configuration section for the database options.
        /// </summary>
        public const string SectionName = "DbOptions";

        /// <summary>MongoDB connection string.</summary>
        public string? ConnectionString { get; set; }

        /// <summary>Name of the database.</summary>
        public string? DatabaseName { get; set; }
    }
}
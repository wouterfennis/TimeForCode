using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Infrastructure.Options
{
    public class DbOptions
    {
        public const string SectionName = "DbOptions";

        [Required]
        public required string ConnectionString { get; init; }

        [Required]
        public required string DatabaseName { get; init; }
    }
}
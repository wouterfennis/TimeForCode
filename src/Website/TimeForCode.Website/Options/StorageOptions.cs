using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Website.Options
{
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class StorageOptions
    {
        public const string SectionName = "StorageOptions";

        [Required]
        public required string StoragePath { get; init; }

        public static StorageOptions Bind(IConfiguration configuration)
        {
            var storageOptions = new StorageOptions
            {
                StoragePath = configuration.GetSection(SectionName).GetValue<string>(nameof(StoragePath))!,
            };

            return storageOptions;
        }
    }
}
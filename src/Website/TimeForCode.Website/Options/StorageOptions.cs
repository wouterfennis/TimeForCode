using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Website.Options
{
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
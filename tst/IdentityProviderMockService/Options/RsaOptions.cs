using System.ComponentModel.DataAnnotations;

namespace IdentityProviderMockService.Options
{
    public class RsaOptions
    {
        public const string SectionName = "RsaOptions";

        [Required]
        public required string Base64Certificate { get; init; }

        public static RsaOptions Bind(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var rsaOptions = new RsaOptions
            {
                Base64Certificate = configuration.GetSection(SectionName).GetValue<string>(nameof(Base64Certificate))!,
            };
            return rsaOptions;
        }
    }
}
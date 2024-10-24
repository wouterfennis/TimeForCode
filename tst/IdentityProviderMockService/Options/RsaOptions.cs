using System.ComponentModel.DataAnnotations;

namespace IdentityProviderMockService.Options
{
    public class RsaOptions
    {
        public const string SectionName = "RsaOptions";

        [Required]
        public string Base64Certificate { get; set; }
    }
}
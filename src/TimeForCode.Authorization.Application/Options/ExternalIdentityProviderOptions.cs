using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Application.Options
{
    public class ExternalIdentityProviderOptions
    {
        public const string SectionName = "ExternalIdentityProviderOptions";

        [Required]
        public required string CallbackUri { get; init; }

        [Required]
        public required ExternalIdentityProvider Github { get; init; }
    }
}
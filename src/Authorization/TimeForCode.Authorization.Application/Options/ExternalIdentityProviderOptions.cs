using System.ComponentModel.DataAnnotations;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Options
{
    public class ExternalIdentityProviderOptions
    {
        public const string SectionName = "ExternalIdentityProviderOptions";

        [Required]
        public required string CallbackUri { get; init; }

        [Required]
        public required ExternalIdentityProvider Github { get; init; }

        public ExternalIdentityProvider GetExternalIdentityProvider(IdentityProvider identityProvider)
        {
            return identityProvider switch
            {
                IdentityProvider.Github => Github,
                _ => throw new NotImplementedException()
            };
        }
    }
}
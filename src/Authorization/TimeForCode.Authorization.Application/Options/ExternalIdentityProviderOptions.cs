using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Options
{
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
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
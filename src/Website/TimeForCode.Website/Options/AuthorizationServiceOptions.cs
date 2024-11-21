using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Website.Options
{
    public class AuthorizationServiceOptions
    {
        public const string SectionName = "AuthorizationServiceOptions";

        [Required]
        public required string BaseUri { get; init; }

        public static AuthorizationServiceOptions Bind(IConfiguration configuration)
        {
            var authenticationOptions = new AuthorizationServiceOptions
            {
                BaseUri = configuration.GetSection(SectionName).GetValue<string>(nameof(BaseUri))!,
            };

            return authenticationOptions;
        }
    }
}

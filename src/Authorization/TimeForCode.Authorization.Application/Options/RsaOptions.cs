using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Authorization.Application.Options
{
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class RsaOptions
    {
        public const string SectionName = "RsaOptions";

        /// <summary>
        /// Signing certificate; only optional in Development, where a throwaway key is generated.
        /// </summary>
        public string? Base64Certificate { get; set; }

        /// <summary>
        /// Optional certificate of the key that was replaced; its public key stays published so earlier tokens remain valid.
        /// </summary>
        public string? PreviousBase64Certificate { get; set; }

        public static RsaOptions Bind(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName);
            var rsaOptions = new RsaOptions
            {
                Base64Certificate = section.GetValue<string>(nameof(Base64Certificate)),
                PreviousBase64Certificate = section.GetValue<string>(nameof(PreviousBase64Certificate)),
            };
            return rsaOptions;
        }
    }
}
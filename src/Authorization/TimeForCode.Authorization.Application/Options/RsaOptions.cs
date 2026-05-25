using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Authorization.Application.Options
{
    [ExcludeFromCodeCoverage(Justification = "Configuration POCO")]
    public class RsaOptions
    {
        public const string SectionName = "RsaOptions";

        [Required]
        public required string Base64Certificate { get; set; }

        public static RsaOptions Bind(IConfiguration configuration)
        {
            var rsaOptions = new RsaOptions
            {
                Base64Certificate = configuration.GetSection(SectionName).GetValue<string>(nameof(Base64Certificate))!,
            };
            return rsaOptions;
        }
    }
}
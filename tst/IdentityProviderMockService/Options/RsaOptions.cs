using System.ComponentModel.DataAnnotations;

namespace IdentityProviderMockService.Options
{
    /// <summary>
    /// Options for RSA key
    /// </summary>
    public class RsaKeyOptions
    {
        public const string SectionName = "RsaKeyOptions";

        /// <summary>
        /// Modulus for verifying and signing
        /// </summary>
        [Required]
        public required string Modulus { get; set; } = string.Empty;

        /// <summary>
        /// Public exponent for verifying
        /// </summary>
        [Required]
        public required string Exponent { get; set; } = string.Empty;

        /// <summary>
        /// Private exponent for signing
        /// </summary>
        [Required]
        public required string D { get; set; } = string.Empty;
    }
}

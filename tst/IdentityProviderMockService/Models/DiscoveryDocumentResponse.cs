using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IdentityProviderMockService.Models
{
    public class DiscoveryDocumentResponse
    {
        /// <summary>
        /// Token issuer
        /// </summary>
        [JsonPropertyName("issuer")]
        [Required]
        public string Issuer { get; set; }

        /// <summary>
        /// JWKS Uri
        /// </summary>
        [JsonPropertyName("jwks_uri")]
        [Required]
        public string JwksUri { get; set; }
    }
}
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
        public required string Issuer { get; init; }

        /// <summary>
        /// JWKS Uri
        /// </summary>
        [JsonPropertyName("jwks_uri")]
        [Required]
        public required string JwksUri { get; init; }
    }
}
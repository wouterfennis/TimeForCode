using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TimeForCode.Shared.Api.Authentication.Models
{
    /// <summary>
    /// Response model for the discovery document endpoint.
    /// </summary>
    public class DiscoveryDocumentResponse
    {
        /// <summary>
        /// The issuer URL that appears in the <c>iss</c> claim of issued tokens
        /// </summary>
        [JsonPropertyName("issuer")]
        [Required]
        public required string Issuer { get; init; }

        /// <summary>
        /// Absolute URL of the JSON Web Key Set that holds the public keys for verifying token signatures
        /// </summary>
        [JsonPropertyName("jwks_uri")]
        [Required]
        public required string JwksUri { get; init; }
    }
}
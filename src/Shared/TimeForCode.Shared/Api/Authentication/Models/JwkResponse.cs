using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TimeForCode.Shared.Api.Authentication.Models
{
    /// <summary>
    /// Response model for JSON Web Key (JWK).
    /// </summary>
    public class JwkResponse
    {
        /// <summary>
        /// Key type
        /// </summary>
        [JsonPropertyName("kty")]
        [Required]
        public required string KeyType { get; init; }

        /// <summary>
        /// Key identifier, matched against the <c>kid</c> header of issued tokens
        /// </summary>
        [JsonPropertyName("kid")]
        [Required]
        public required string KeyId { get; init; }

        /// <summary>
        /// Intended use of the key (<c>sig</c> for signature verification)
        /// </summary>
        [JsonPropertyName("use")]
        [Required]
        public required string Use { get; init; }

        /// <summary>
        /// Signing algorithm the key is used with
        /// </summary>
        [JsonPropertyName("alg")]
        [Required]
        public required string Algorithm { get; init; }

        /// <summary>
        /// JWKS Uri
        /// </summary>
        [JsonPropertyName("e")]
        [Required]
        public required string Exponent { get; init; }

        /// <summary>
        /// Modulus
        /// </summary>
        [JsonPropertyName("n")]
        [Required]
        public required string Modulus { get; init; }
    }
}
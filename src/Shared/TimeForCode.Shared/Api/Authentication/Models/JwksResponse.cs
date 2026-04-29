using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TimeForCode.Shared.Api.Authentication.Models
{
    /// <summary>
    /// Response model for JSON Web Key Set (JWKS).
    /// </summary>
    public class JwksResponse
    {
        /// <summary>
        /// JWK keys
        /// </summary>
        [JsonPropertyName("keys")]
        [Required]
        public required IEnumerable<JwkResponse> Keys { get; set; }
    }
}
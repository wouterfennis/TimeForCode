using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IdentityProviderMockService.Models
{
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
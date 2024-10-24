using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IdentityProviderMockService.Models
{
    public class AccessTokenRequest
    {
        /// <summary>
        /// Client id
        /// </summary>
        [JsonPropertyName("client_id")]
        [Required]
        public required string ClientId { get; init; }

        /// <summary>
        /// Client secret
        /// </summary>
        [JsonPropertyName("client_secret")]
        [Required]
        public required string ClientSecret { get; init; }

        /// <summary>
        /// Authorization code
        /// </summary>
        [Required]
        public required string Code { get; init; }
    }
}

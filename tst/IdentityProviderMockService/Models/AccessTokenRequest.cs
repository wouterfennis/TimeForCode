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
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret
        /// </summary>
        [JsonPropertyName("client_secret")]
        [Required]
        public string ClientSecret { get; set; }

        /// <summary>
        /// Authorization code
        /// </summary>
        [Required]
        public string Code { get; set; }
    }
}

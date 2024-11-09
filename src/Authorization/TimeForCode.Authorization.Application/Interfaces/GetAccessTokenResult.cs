using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public class GetAccessTokenResult
    {
        /// <summary>
        /// Token Type
        /// </summary>
        [JsonPropertyName("token_type")]
        [Required]
        public required string TokenType { get; init; }

        /// <summary>
        /// Access Token
        /// </summary>
        [JsonPropertyName("access_token")]
        [Required]
        public required string AccessToken { get; init; }

        /// <summary>
        /// Scope
        /// </summary>
        [JsonPropertyName("scope")]
        [Required]
        public required string Scope { get; init; }
    }
}
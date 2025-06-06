﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IdentityProviderMockService.Models
{
    public class JwkResponse
    {
        /// <summary>
        /// Key type
        /// </summary>
        [JsonPropertyName("kty")]
        [Required]
        public required string KeyType { get; init; }

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
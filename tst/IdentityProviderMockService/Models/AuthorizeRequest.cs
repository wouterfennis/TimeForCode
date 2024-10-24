using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IdentityProviderMockService.Models
{
    public class AuthorizeRequest
    {
        /// <summary>
        /// State
        /// </summary>
        [FromQuery]
        [Required]
        public required string State { get; init; }

        /// <summary>
        /// Redirect Uri
        /// </summary>
        [FromQuery(Name = "redirect_uri")]
        [Required]
        public required string RedirectUri { get; init; }

        /// <summary>
        /// Scope
        /// </summary>
        [FromQuery]
        [Required]
        public required string Scope { get; init; }

        /// <summary>
        /// Client id
        /// </summary>
        [FromQuery(Name = "client_id")]
        [Required]
        public required string ClientId { get; init; }
    }
}

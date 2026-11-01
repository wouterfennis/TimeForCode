using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace IdentityProviderMockService.Models
{
    public class ConfirmRequest
    {
        /// <summary>
        /// State
        /// </summary>
        [FromForm]
        [Required]
        public required string State { get; init; }

        /// <summary>
        /// Redirect Uri
        /// </summary>
        [FromForm(Name = "redirect_uri")]
        [Required]
        public required string RedirectUri { get; init; }

        /// <summary>
        /// Scope
        /// </summary>
        [FromForm]
        public string? Scope { get; init; }

        /// <summary>
        /// Client id
        /// </summary>
        [FromForm(Name = "client_id")]
        [Required]
        public required string ClientId { get; init; }

        /// <summary>
        /// Optional persona that signs in
        /// </summary>
        [FromForm]
        [StringLength(PersonaProfile.MaxLength)]
        [RegularExpression(PersonaProfile.AllowedPattern)]
        public string? Persona { get; init; }
    }
}
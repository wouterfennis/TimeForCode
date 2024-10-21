using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models
{
    /// <summary>
    /// Represents the model for a login request.
    /// </summary>
    public class LoginRequestModel
    {
        /// <summary>
        /// Gets or sets the identity provider for the login request.
        /// </summary>
        [Required]
        [FromQuery]
        public required IdentityProvider IdentityProvider { get; init; }
    }
}
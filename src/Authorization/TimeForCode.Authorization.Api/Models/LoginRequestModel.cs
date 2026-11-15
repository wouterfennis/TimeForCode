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

        /// <summary>
        /// Gets or sets the redirect URI for the login request.
        /// </summary>
        [Required]
        [FromQuery]
        public required Uri RedirectUri { get; init; }

        /// <summary>
        /// Gets or sets the PKCE code challenge: the unpadded base64url SHA-256 hash (43 characters, method S256) of a random verifier
        /// that only the client holds. The code exchange requires that verifier, which binds the sign-in to the client that started it.
        /// </summary>
        [Required]
        [FromQuery]
        [RegularExpression("^[A-Za-z0-9_-]{43}$")]
        public required string CodeChallenge { get; init; }
    }
}
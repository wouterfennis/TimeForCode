using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models
{
    /// <summary>
    /// Represents the request to exchange the single-use code of a completed sign-in for tokens.
    /// </summary>
    public class ExchangeCodeRequestModel
    {
        /// <summary>
        /// The single-use code the browser received in the redirect after sign-in. It is valid once and for a short time.
        /// </summary>
        [Required]
        [MaxLength(ApiConstraints.MaxOAuthParameterLength)]
        public required string Code { get; init; }

        /// <summary>
        /// The PKCE code verifier (43 to 128 unreserved characters) whose S256 challenge was sent when the login started.
        /// A code started with another verifier is rejected.
        /// </summary>
        [Required]
        [MinLength(43)]
        [MaxLength(128)]
        public required string CodeVerifier { get; init; }
    }
}
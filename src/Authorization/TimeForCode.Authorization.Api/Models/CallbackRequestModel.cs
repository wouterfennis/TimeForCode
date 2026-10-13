using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models
{
    /// <summary>
    /// Represents the model for a callback request.
    /// </summary>
    public class CallbackRequestModel
    {
        /// <summary>
        /// Gets or sets the authorization code.
        /// </summary>
        [Required]
        [MaxLength(ApiConstraints.MaxOAuthParameterLength)]
        public required string Code { get; init; }

        /// <summary>
        /// Gets or sets the state parameter.
        /// </summary>
        [Required]
        [MaxLength(ApiConstraints.MaxOAuthParameterLength)]
        public required string State { get; init; }
    }
}
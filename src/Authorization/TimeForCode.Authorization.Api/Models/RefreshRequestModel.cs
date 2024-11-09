using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models
{
    /// <summary>
    /// Represents the model for a refresh request.
    /// </summary>
    public class RefreshRequestModel
    {
        /// <summary>
        /// The refresh token.
        /// </summary>
        [Required]
        public required string RefreshToken { get; init; }
    }
}
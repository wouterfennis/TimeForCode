using System.ComponentModel.DataAnnotations;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Api.Models
{
    /// <summary>
    /// Represents a request that carries the refresh token: renewing the tokens or signing out.
    /// </summary>
    public class RefreshRequestModel
    {
        /// <summary>
        /// The refresh token the client holds.
        /// </summary>
        [Required]
        public required RefreshToken RefreshToken { get; init; }
    }
}
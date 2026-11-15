using System.ComponentModel.DataAnnotations;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Models
{
    /// <summary>
    /// The tokens issued to a client: after a code exchange or a refresh. The client keeps them server-side and never hands them to a browser script.
    /// </summary>
    public class TokenResponseModel
    {
        /// <summary>
        /// The short-lived access token; send it as a bearer token.
        /// </summary>
        [Required]
        public required AccessToken AccessToken { get; init; }

        /// <summary>
        /// The new refresh token. Single use: the one the client sent to refresh is now invalid.
        /// </summary>
        [Required]
        public required RefreshToken RefreshToken { get; init; }

        /// <summary>
        /// Indicates whether this is the user's first login (new account was created).
        /// </summary>
        public bool IsNewUser { get; init; }
    }
}
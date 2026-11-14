using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models
{

    /// <summary>
    /// Represents the model for a sign-in or refresh response. It carries no token: both tokens are only in HttpOnly cookies (ADR-005).
    /// </summary>
    public class CallbackResponseModel
    {
        /// <summary>
        /// When the new access token, which only the HttpOnly cookie carries, expires. The client schedules its next refresh from this.
        /// </summary>
        [Required]
        public required DateTimeOffset ExpiresAfter { get; init; }

        /// <summary>
        /// Indicates whether this is the user's first login (new account was created).
        /// </summary>
        public bool IsNewUser { get; init; }
    }
}
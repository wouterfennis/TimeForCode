using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models.Admin
{
    /// <summary>
    /// Completes the admin passkey authentication ceremony.
    /// </summary>
    public class AdminCompleteAuthenticationRequestModel
    {
        /// <summary>
        /// The JSON-serialised <c>PublicKeyCredential</c> returned by <c>navigator.credentials.get()</c>.
        /// </summary>
        [Required]
        public required string CredentialJson { get; init; }
    }
}
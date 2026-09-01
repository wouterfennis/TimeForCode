using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models.Admin
{
    /// <summary>
    /// Completes the one-time admin passkey registration ceremony. The bootstrap secret is re-validated here.
    /// </summary>
    public class AdminCompleteRegistrationRequestModel
    {
        /// <summary>
        /// Shared secret guarding who may claim the admin role, re-validated on completion.
        /// </summary>
        [Required]
        public required string BootstrapSecret { get; init; }

        /// <summary>
        /// The JSON-serialised <c>PublicKeyCredential</c> returned by <c>navigator.credentials.create()</c>.
        /// </summary>
        [Required]
        public required string CredentialJson { get; init; }
    }
}
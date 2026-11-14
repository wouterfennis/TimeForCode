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
        [MaxLength(ApiConstraints.MaxCredentialJsonLength)]
        public required string CredentialJson { get; init; }

        /// <summary>
        /// The ceremony id returned by the options call. It works once and expires after a short time.
        /// </summary>
        [Required]
        [MaxLength(ApiConstraints.MaxCeremonyIdLength)]
        public required string CeremonyId { get; init; }
    }
}
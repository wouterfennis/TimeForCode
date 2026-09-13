using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models.Admin
{
    /// <summary>
    /// WebAuthn creation/request options for the browser to pass to <c>navigator.credentials</c>.
    /// </summary>
    public class AdminPasskeyOptionsResponseModel
    {
        /// <summary>
        /// The JSON-serialised WebAuthn options for the browser to pass to <c>navigator.credentials</c>.
        /// </summary>
        [Required]
        public required string OptionsJson { get; init; }
    }
}
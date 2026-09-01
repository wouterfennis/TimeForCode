using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models.Admin
{
    /// <summary>
    /// Request to begin the one-time admin passkey registration ceremony.
    /// </summary>
    public class AdminRegistrationOptionsRequestModel
    {
        /// <summary>
        /// Shared secret guarding who may claim the admin role.
        /// </summary>
        [Required]
        public required string BootstrapSecret { get; init; }
    }
}

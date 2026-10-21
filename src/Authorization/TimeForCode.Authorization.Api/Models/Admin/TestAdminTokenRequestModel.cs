using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models.Admin
{
    /// <summary>
    /// Request for a test-only admin token.
    /// </summary>
    public class TestAdminTokenRequestModel
    {
        /// <summary>
        /// Shared secret configured for the test-only admin access.
        /// </summary>
        [Required]
        [MaxLength(ApiConstraints.MaxSecretLength)]
        public required string Secret { get; init; }
    }
}
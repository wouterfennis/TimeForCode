using System.ComponentModel.DataAnnotations;

namespace IdentityProviderMockService.Models
{
    public class AuthorizeDetails
    {
        /// <summary>
        /// Client id
        /// </summary>
        [Required]
        public string ClientId { get; set; }

        /// <summary>
        /// Scope
        /// </summary>
        [Required]
        public string Scope { get; set; }
    }
}

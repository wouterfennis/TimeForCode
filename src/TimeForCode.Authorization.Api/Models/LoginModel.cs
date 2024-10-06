using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models
{
    public class LoginModel
    {
        [Required]
        public required IdentityProvider IdentityProvider { get; set; }
    }
}

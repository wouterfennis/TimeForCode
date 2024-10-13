using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Api.Models
{
    public class LoginRequestModel
    {
        [Required]
        public required IdentityProvider IdentityProvider { get; init; }
    }
}

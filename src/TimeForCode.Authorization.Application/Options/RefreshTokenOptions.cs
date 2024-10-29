using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Authorization.Application.Options
{
    public class RefreshTokenOptions
    {
        public const string SectionName = "RefreshTokenOptions";

        [Required]
        public required int DefaultExpirationAfterInDays { get; init; }
    }
}
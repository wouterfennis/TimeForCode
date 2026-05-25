using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Donation.Infrastructure.Options
{
    public class GithubApiOptions
    {
        public const string SectionName = "GithubApiOptions";

        [Required]
        public required string BaseUrl { get; init; }
    }
}
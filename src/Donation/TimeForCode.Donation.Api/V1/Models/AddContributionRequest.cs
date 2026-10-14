using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a request to log time spent on a project.
    /// </summary>
    public class AddContributionRequest
    {
        /// <summary>
        /// The whole number of hours spent on the project.
        /// </summary>
        [Range(1, ApiConstraints.MaxContributionHours)]
        public required int Hours { get; init; }

        /// <summary>
        /// Optional evidence: an HTTPS link to github.com, for example a pull request or a commit.
        /// </summary>
        public Uri? GithubReference { get; init; }
    }
}
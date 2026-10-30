using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a request to correct a logged time contribution. It replaces both values: omitting the reference removes it.
    /// </summary>
    public class CorrectContributionRequest
    {
        /// <summary>
        /// The corrected whole number of hours spent on the project.
        /// </summary>
        [Range(1, ApiConstraints.MaxContributionHours)]
        public required int Hours { get; init; }

        /// <summary>
        /// Optional evidence: an HTTPS link to github.com, for example a pull request or a commit.
        /// </summary>
        public Uri? GithubReference { get; init; }
    }
}
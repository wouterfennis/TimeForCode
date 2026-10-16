namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a time contribution logged on a project.
    /// </summary>
    public class ContributionResponse
    {
        /// <summary>The contribution identifier.</summary>
        public required string Id { get; init; }

        /// <summary>The identifier of the project the time was logged on.</summary>
        public required string ProjectId { get; init; }

        /// <summary>The identifier of the contributor who logged the time.</summary>
        public required string ContributorId { get; init; }

        /// <summary>The GitHub login of the contributor at the time of logging.</summary>
        public required string ContributorGitHubLogin { get; init; }

        /// <summary>The avatar URL of the contributor at the time of logging.</summary>
        public required string ContributorAvatarUrl { get; init; }

        /// <summary>The number of hours logged.</summary>
        public required int Hours { get; init; }

        /// <summary>Optional link to a GitHub pull request or commit that evidences the work.</summary>
        public string? GithubReference { get; init; }

        /// <summary>The moment the hours were logged.</summary>
        public required DateTimeOffset LoggedAt { get; init; }
    }
}
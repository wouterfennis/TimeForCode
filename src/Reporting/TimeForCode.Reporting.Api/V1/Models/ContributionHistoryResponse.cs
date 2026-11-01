namespace TimeForCode.Reporting.Api.V1.Models
{
    /// <summary>
    /// The JSON shape of the contribution history export. The CSV export has the same values in the columns
    /// ContributionId, ProjectId, ContributorLogin, Hours, GithubReference and LoggedAt.
    /// </summary>
    public class ContributionHistoryResponse
    {
        /// <summary>The contributions, earliest first.</summary>
        public required IReadOnlyList<ContributionHistoryItemResponse> Contributions { get; init; }
    }

    /// <summary>
    /// One logged contribution in the export.
    /// </summary>
    public class ContributionHistoryItemResponse
    {
        /// <summary>The contribution identifier.</summary>
        public required string Id { get; init; }

        /// <summary>The project the hours were logged on.</summary>
        public required string ProjectId { get; init; }

        /// <summary>GitHub login of the contributor.</summary>
        public required string ContributorLogin { get; init; }

        /// <summary>The hours logged.</summary>
        public required int Hours { get; init; }

        /// <summary>The GitHub issue or pull request the hours belong to; <c>null</c> when none was given.</summary>
        public string? GithubReference { get; init; }

        /// <summary>When the hours were logged, in UTC.</summary>
        public required DateTimeOffset LoggedAt { get; init; }
    }
}
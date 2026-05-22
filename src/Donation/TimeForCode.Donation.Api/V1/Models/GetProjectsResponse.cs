namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a paginated list of published projects.
    /// </summary>
    public class GetProjectsResponse
    {
        /// <summary>The list of projects for the current page.</summary>
        public required IReadOnlyList<ProjectSummaryResponse> Projects { get; init; }

        /// <summary>The total number of published projects.</summary>
        public required int TotalCount { get; init; }

        /// <summary>The current page number (1-based).</summary>
        public required int PageNumber { get; init; }

        /// <summary>The number of items per page.</summary>
        public required int PageSize { get; init; }
    }
}
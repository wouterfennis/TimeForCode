namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents one page of the contributions logged by the contributors of a donor organization.
    /// </summary>
    public class GetDonorOrganizationContributionsResponse
    {
        /// <summary>The contributions of this page, earliest first.</summary>
        public required IReadOnlyList<ContributionResponse> Contributions { get; init; }

        /// <summary>The total number of contributions of the organization.</summary>
        public required int TotalCount { get; init; }

        /// <summary>The page number, starting at 1.</summary>
        public required int PageNumber { get; init; }

        /// <summary>The maximum number of contributions per page.</summary>
        public required int PageSize { get; init; }
    }
}
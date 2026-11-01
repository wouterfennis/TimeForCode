namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents one page of the donations pledged for a donor organization.
    /// </summary>
    public class GetDonorOrganizationDonationsResponse
    {
        /// <summary>The donations of this page, oldest pledge first.</summary>
        public required IReadOnlyList<DonationResponse> Donations { get; init; }

        /// <summary>The total number of donations of the organization.</summary>
        public required int TotalCount { get; init; }

        /// <summary>The page number, starting at 1.</summary>
        public required int PageNumber { get; init; }

        /// <summary>The maximum number of donations per page.</summary>
        public required int PageSize { get; init; }
    }
}
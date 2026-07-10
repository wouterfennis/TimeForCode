namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a paginated list of donor organizations.
    /// </summary>
    public class GetDonorOrganizationsResponse
    {
        /// <summary>
        /// The list of donor organizations.
        /// </summary>
        public required List<DonorOrganizationResponse> Organizations { get; init; }

        /// <summary>
        /// The total count of donor organizations.
        /// </summary>
        public required int TotalCount { get; init; }

        /// <summary>
        /// The current page number.
        /// </summary>
        public required int PageNumber { get; init; }

        /// <summary>
        /// The page size.
        /// </summary>
        public required int PageSize { get; init; }
    }
}
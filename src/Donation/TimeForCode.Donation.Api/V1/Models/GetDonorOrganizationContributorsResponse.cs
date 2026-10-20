namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents the contributors affiliated with a donor organization.
    /// </summary>
    public class GetDonorOrganizationContributorsResponse
    {
        /// <summary>The affiliated contributors, ordered by GitHub login; empty when there are none.</summary>
        public required IReadOnlyList<ContributorProfileResponse> Contributors { get; init; }
    }
}
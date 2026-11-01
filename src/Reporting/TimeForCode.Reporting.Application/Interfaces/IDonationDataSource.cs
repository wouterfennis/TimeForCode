using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;

namespace TimeForCode.Reporting.Application.Interfaces
{
    /// <summary>
    /// Read-only view on the Donation context. Access is decided by the Donation context for the caller's own access token:
    /// a failure carries the status it answered with (unauthorized, forbidden or not found).
    /// </summary>
    public interface IDonationDataSource
    {
        /// <summary>
        /// Returns the donations pledged for the donor organization.
        /// </summary>
        Task<Result<IReadOnlyList<OrganizationDonation>>> GetDonationsAsync(string donorOrganizationId, string accessToken, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the contributions logged by the contributors of the donor organization, earliest first.
        /// </summary>
        Task<Result<IReadOnlyList<OrganizationContribution>>> GetContributionsAsync(string donorOrganizationId, string accessToken, CancellationToken cancellationToken);
    }
}
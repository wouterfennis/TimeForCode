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

        /// <summary>
        /// Checks that the caller of the access token maintains the project. Fails with unauthorized, forbidden (not the maintainer) or not found.
        /// </summary>
        Task<Result<bool>> VerifyMaintainerAsync(string projectId, string accessToken, CancellationToken cancellationToken);

        /// <summary>
        /// Returns every donation pledged to a published project, whatever its status. Public in the Donation context, so no access token.
        /// Fails with not found for an unknown or unpublished project.
        /// </summary>
        Task<Result<IReadOnlyList<ProjectDonation>>> GetProjectDonationsAsync(string projectId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the contributions logged on a published project, earliest first. Public in the Donation context, so no access token.
        /// Fails with not found for an unknown or unpublished project.
        /// </summary>
        Task<Result<IReadOnlyList<ProjectContribution>>> GetProjectContributionsAsync(string projectId, CancellationToken cancellationToken);
    }
}
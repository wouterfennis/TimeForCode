using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    /// <summary>
    /// The access rule for reading the data of a donor organization: the organization must exist, and the caller must be
    /// an administrator or a contributor affiliated with it.
    /// </summary>
    internal static class DonorOrganizationAccess
    {
        /// <summary>
        /// Returns the failure to hand back to the caller (not found or forbidden), or <c>null</c> when the caller may read the organization.
        /// </summary>
        internal static async Task<Result<T>?> CheckAsync<T>(
            IDonorOrganizationRepository organizationRepository,
            IContributorRepository contributorRepository,
            string donorOrganizationId,
            string callerUserId,
            bool isAdministrator,
            ILogger logger)
        {
            var organization = await organizationRepository.GetByIdAsync(donorOrganizationId);
            if (organization == null)
            {
                logger.LogWarning("Donor organization {DonorOrganizationId} not found", donorOrganizationId);
                return Result<T>.NotFound("Donor organization not found.");
            }

            if (isAdministrator)
            {
                return null;
            }

            var caller = await contributorRepository.GetByPlatformUserIdAsync(callerUserId);
            if (caller?.DonorOrganizationId?.ToString() != donorOrganizationId)
            {
                logger.LogWarning("User {UserId} is not a member of donor organization {DonorOrganizationId}", callerUserId, donorOrganizationId);
                return Result<T>.Forbidden("Only members of the donor organization or an administrator can read its data.");
            }

            return null;
        }
    }
}
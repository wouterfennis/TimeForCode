using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IDonationRepository
    {
        Task CreateAsync(Domain.Donation donation);
        Task<Domain.Donation?> GetByIdAsync(string id);
        Task UpdateAsync(Domain.Donation donation);

        /// <summary>
        /// Returns the donations pledged by the user, oldest first.
        /// </summary>
        Task<IReadOnlyList<Domain.Donation>> GetByPledgedByUserIdAsync(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the donations pledged to the project, oldest first.
        /// </summary>
        Task<IReadOnlyList<Domain.Donation>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the donations pledged for any of the donor organizations.
        /// </summary>
        Task<IReadOnlyList<Domain.Donation>> GetByDonorOrganizationIdsAsync(IReadOnlyCollection<string> organizationIds, CancellationToken cancellationToken);
    }
}
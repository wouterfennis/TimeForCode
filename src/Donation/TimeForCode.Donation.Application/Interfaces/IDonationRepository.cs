using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IDonationRepository
    {
        Task CreateAsync(Domain.Donation donation);
        Task<Domain.Donation?> GetByIdAsync(string id);

        /// <summary>
        /// Saves the status and logged hours of the donation.
        /// </summary>
        /// <exception cref="Exceptions.RepositoryConflictException">The donation was changed by someone else since it was loaded.</exception>
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
        /// Returns the Active donations of the project, oldest first, without the listing cap so every Active donation can receive hours.
        /// </summary>
        Task<IReadOnlyList<Domain.Donation>> GetActiveByProjectIdAsync(string projectId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the Active and Completed donations pledged to any of the projects, without the listing cap so totals stay correct.
        /// </summary>
        Task<IReadOnlyList<Domain.Donation>> GetReceivedByProjectIdsAsync(IReadOnlyCollection<string> projectIds, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the donations pledged for any of the donor organizations.
        /// </summary>
        Task<IReadOnlyList<Domain.Donation>> GetByDonorOrganizationIdsAsync(IReadOnlyCollection<string> organizationIds, CancellationToken cancellationToken);
    }
}
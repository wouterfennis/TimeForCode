using MongoDB.Bson;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IContributionRepository
    {
        Task CreateAsync(Contribution contribution);
        Task<IReadOnlyList<Contribution>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the contribution of the project, or <c>null</c> when there is none or it was deleted.
        /// </summary>
        Task<Contribution?> GetByIdAsync(string projectId, string contributionId, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the stored contribution with the same id, including its history and deleted marker.
        /// </summary>
        Task UpdateAsync(Contribution contribution, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the contributor login and avatar snapshot on every contribution of the contributor with those of a deleted user.
        /// </summary>
        Task AnonymiseByContributorIdAsync(ObjectId contributorId, CancellationToken cancellationToken);
    }
}
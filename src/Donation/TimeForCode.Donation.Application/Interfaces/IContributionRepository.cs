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
        /// Saves the hours, reference, history and deleted marker of the contribution.
        /// </summary>
        /// <exception cref="Exceptions.RepositoryConflictException">The contribution was changed by someone else since it was loaded.</exception>
        Task UpdateAsync(Contribution contribution, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the contributor login and avatar snapshot on every contribution of the contributor with those of a deleted user.
        /// </summary>
        Task AnonymiseByContributorIdAsync(ObjectId contributorId, CancellationToken cancellationToken);
    }
}
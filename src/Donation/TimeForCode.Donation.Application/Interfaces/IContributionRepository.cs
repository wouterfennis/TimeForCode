using MongoDB.Bson;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IContributionRepository
    {
        Task CreateAsync(Contribution contribution);
        Task<IReadOnlyList<Contribution>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the contributor login and avatar snapshot on every contribution of the contributor with those of a deleted user.
        /// </summary>
        Task AnonymiseByContributorIdAsync(ObjectId contributorId, CancellationToken cancellationToken);
    }
}
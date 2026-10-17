using MongoDB.Bson;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IContributorRepository
    {
        /// <summary>
        /// Inserts the contributor, or refreshes the GitHub login and avatar of the existing one with the same
        /// platform user id; returns the persisted document (with its stable id).
        /// </summary>
        Task<Contributor> UpsertAsync(Contributor contributor);

        /// <summary>
        /// Returns the contributor of the platform user, or <c>null</c> when the user never logged time or was anonymised.
        /// </summary>
        Task<Contributor?> GetByPlatformUserIdAsync(string platformUserId);

        /// <summary>
        /// Replaces the platform user id, GitHub login and avatar of the contributor with those of a deleted user.
        /// </summary>
        Task AnonymiseAsync(ObjectId contributorId, CancellationToken cancellationToken);
    }
}
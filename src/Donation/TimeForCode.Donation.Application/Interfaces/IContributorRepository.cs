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
        /// Returns the contributor with the identifier, or <c>null</c> when there is none.
        /// </summary>
        Task<Contributor?> GetByIdAsync(ObjectId contributorId);

        /// <summary>
        /// Returns the contributors affiliated with the donor organization, in a stable order.
        /// </summary>
        Task<IReadOnlyList<Contributor>> GetByDonorOrganizationIdAsync(ObjectId donorOrganizationId, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the profile fields (role, skills, hours available, donor organization) of the contributor; the identity fields are left alone.
        /// </summary>
        Task UpdateProfileAsync(Contributor contributor, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the platform user id, GitHub login and avatar of the contributor with those of a deleted user.
        /// </summary>
        Task AnonymiseAsync(ObjectId contributorId, CancellationToken cancellationToken);
    }
}
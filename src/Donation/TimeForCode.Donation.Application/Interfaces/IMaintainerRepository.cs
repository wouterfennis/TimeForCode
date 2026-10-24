using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IMaintainerRepository
    {
        /// <summary>
        /// Inserts the maintainer, or refreshes the GitHub login and avatar of the existing one with the same
        /// platform user id; returns the persisted document (with its stable id).
        /// </summary>
        Task<Maintainer> UpsertAsync(Maintainer maintainer);

        /// <summary>
        /// Returns the maintainer of the platform user, or <c>null</c> when the user never submitted a project.
        /// </summary>
        Task<Maintainer?> GetByPlatformUserIdAsync(string platformUserId);
    }
}
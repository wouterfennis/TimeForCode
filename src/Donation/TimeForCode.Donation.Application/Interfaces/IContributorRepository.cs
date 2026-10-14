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
    }
}
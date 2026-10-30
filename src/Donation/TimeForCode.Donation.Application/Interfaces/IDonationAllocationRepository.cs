using MongoDB.Bson;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IDonationAllocationRepository
    {
        Task CreateAsync(DonationAllocation allocation, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the allocations of the contribution, oldest first.
        /// </summary>
        Task<IReadOnlyList<DonationAllocation>> GetByContributionIdAsync(ObjectId contributionId, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the reduced hours of an allocation after part of them was released.
        /// </summary>
        Task UpdateAsync(DonationAllocation allocation, CancellationToken cancellationToken);

        /// <summary>
        /// Removes an allocation whose donation update did not go through, or whose hours were all released.
        /// </summary>
        Task DeleteAsync(ObjectId id, CancellationToken cancellationToken);
    }
}
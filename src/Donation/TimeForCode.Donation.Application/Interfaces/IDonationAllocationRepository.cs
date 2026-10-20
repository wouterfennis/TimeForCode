using MongoDB.Bson;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IDonationAllocationRepository
    {
        Task CreateAsync(DonationAllocation allocation, CancellationToken cancellationToken);

        /// <summary>
        /// Removes an allocation whose donation update did not go through.
        /// </summary>
        Task DeleteAsync(ObjectId id, CancellationToken cancellationToken);
    }
}
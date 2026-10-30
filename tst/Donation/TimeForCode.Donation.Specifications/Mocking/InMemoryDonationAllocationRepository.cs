using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// In-memory stand-in for the donation allocation collection.
    /// </summary>
    internal class InMemoryDonationAllocationRepository : IDonationAllocationRepository
    {
        private readonly List<DonationAllocation> _allocations = [];

        internal void Clear()
        {
            _allocations.Clear();
        }

        internal IReadOnlyList<DonationAllocation> Allocations => _allocations;

        public Task CreateAsync(DonationAllocation allocation, CancellationToken cancellationToken)
        {
            _allocations.Add(allocation);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DonationAllocation>> GetByContributionIdAsync(ObjectId contributionId, CancellationToken cancellationToken)
        {
            IReadOnlyList<DonationAllocation> matches = _allocations
                .Where(a => a.ContributionId == contributionId)
                .OrderBy(a => a.CreatedAt)
                .ThenBy(a => a.Id)
                .ToList();

            return Task.FromResult(matches);
        }

        public Task UpdateAsync(DonationAllocation allocation, CancellationToken cancellationToken)
        {
            _allocations[_allocations.FindIndex(a => a.Id == allocation.Id)] = allocation;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ObjectId id, CancellationToken cancellationToken)
        {
            _allocations.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }
    }
}
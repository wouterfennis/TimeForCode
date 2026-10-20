using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class DonationAllocationRepository : IDonationAllocationRepository
    {
        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<DonationAllocation> _collection;

        public DonationAllocationRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<DonationAllocation>();
            EnsureIndexes(_collection);
        }

        private static void EnsureIndexes(IMongoCollection<DonationAllocation> collection)
        {
            if (_indexesCreated)
            {
                return;
            }

            lock (IndexCreationLock)
            {
                if (_indexesCreated)
                {
                    return;
                }

                collection.Indexes.CreateMany(
                [
                    new CreateIndexModel<DonationAllocation>(
                        Builders<DonationAllocation>.IndexKeys.Ascending(a => a.ContributionId),
                        new CreateIndexOptions { Name = "ix_donation_allocations_contribution" }),
                    new CreateIndexModel<DonationAllocation>(
                        Builders<DonationAllocation>.IndexKeys.Ascending(a => a.DonationId),
                        new CreateIndexOptions { Name = "ix_donation_allocations_donation" })
                ]);
                _indexesCreated = true;
            }
        }

        public async Task CreateAsync(DonationAllocation allocation, CancellationToken cancellationToken)
        {
            await _collection.InsertOneAsync(allocation, cancellationToken: cancellationToken);
        }

        public async Task DeleteAsync(ObjectId id, CancellationToken cancellationToken)
        {
            await _collection.DeleteOneAsync(Builders<DonationAllocation>.Filter.Eq(a => a.Id, id), cancellationToken);
        }
    }
}
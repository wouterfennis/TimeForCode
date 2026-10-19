using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Interfaces;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class DonationRepository : IDonationRepository
    {
        // Upper bound for one listing; keeps the unpaged endpoints from reading an unbounded collection. The oldest pledges win.
        private const int MaxDonationsPerQuery = 1000;

        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<Domain.Donation> _collection;

        public DonationRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<Domain.Donation>();
            EnsureIndexes(_collection);
        }

        private static void EnsureIndexes(IMongoCollection<Domain.Donation> collection)
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

                var indexKeys = Builders<Domain.Donation>.IndexKeys;
                collection.Indexes.CreateMany(
                [
                    new CreateIndexModel<Domain.Donation>(
                        indexKeys.Ascending(d => d.ProjectId).Ascending(d => d.CreatedAt),
                        new CreateIndexOptions { Name = "ix_donations_project_created_at" }),
                    new CreateIndexModel<Domain.Donation>(
                        indexKeys.Ascending(d => d.PledgedByUserId).Ascending(d => d.CreatedAt),
                        new CreateIndexOptions { Name = "ix_donations_pledged_by_created_at" }),
                    new CreateIndexModel<Domain.Donation>(
                        indexKeys.Ascending(d => d.DonorOrganizationId),
                        new CreateIndexOptions { Name = "ix_donations_donor_organization" })
                ]);
                _indexesCreated = true;
            }
        }

        public async Task CreateAsync(Domain.Donation donation)
        {
            await _collection.InsertOneAsync(donation);
        }

        public async Task<Domain.Donation?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return null;
            }

            return await _collection
                .Find(Builders<Domain.Donation>.Filter.Eq(d => d.Id, objectId))
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Domain.Donation donation)
        {
            var update = Builders<Domain.Donation>.Update
                .Set(d => d.Status, donation.Status)
                .Set(d => d.HoursLogged, donation.HoursLogged)
                .Set(d => d.UpdatedAt, donation.UpdatedAt);

            await _collection.UpdateOneAsync(Builders<Domain.Donation>.Filter.Eq(d => d.Id, donation.Id), update);
        }

        public async Task<IReadOnlyList<Domain.Donation>> GetByPledgedByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await FindOldestFirstAsync(Builders<Domain.Donation>.Filter.Eq(d => d.PledgedByUserId, userId), cancellationToken);
        }

        public async Task<IReadOnlyList<Domain.Donation>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(projectId, out var objectId))
            {
                return [];
            }

            return await FindOldestFirstAsync(Builders<Domain.Donation>.Filter.Eq(d => d.ProjectId, objectId), cancellationToken);
        }

        public async Task<IReadOnlyList<Domain.Donation>> GetByDonorOrganizationIdsAsync(IReadOnlyCollection<string> organizationIds, CancellationToken cancellationToken)
        {
            var objectIds = organizationIds
                .Select(id => ObjectId.TryParse(id, out var objectId) ? (ObjectId?)objectId : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (objectIds.Count == 0)
            {
                return [];
            }

            return await _collection
                .Find(Builders<Domain.Donation>.Filter.In(d => d.DonorOrganizationId, objectIds.Select(id => (ObjectId?)id)))
                .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyList<Domain.Donation>> FindOldestFirstAsync(FilterDefinition<Domain.Donation> filter, CancellationToken cancellationToken)
        {
            return await _collection
                .Find(filter)
                .SortBy(d => d.CreatedAt)
                .ThenBy(d => d.Id)
                .Limit(MaxDonationsPerQuery)
                .ToListAsync(cancellationToken);
        }
    }
}
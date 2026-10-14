using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class ContributionRepository : IContributionRepository
    {
        // Upper bound for one project listing; keeps the unpaged endpoint from reading an unbounded collection.
        private const int MaxContributionsPerProject = 1000;

        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<Contribution> _collection;

        public ContributionRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<Contribution>();
            EnsureIndexes(_collection);
        }

        private static void EnsureIndexes(IMongoCollection<Contribution> collection)
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

                var indexModel = new CreateIndexModel<Contribution>(
                    Builders<Contribution>.IndexKeys
                        .Ascending(c => c.ProjectId)
                        .Ascending(c => c.LoggedAt),
                    new CreateIndexOptions { Name = "ix_contributions_project_logged_at" });

                collection.Indexes.CreateOne(indexModel);
                _indexesCreated = true;
            }
        }

        public async Task CreateAsync(Contribution contribution)
        {
            await _collection.InsertOneAsync(contribution);
        }

        public async Task<IReadOnlyList<Contribution>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(projectId, out var objectId))
            {
                return [];
            }

            return await _collection
                .Find(Builders<Contribution>.Filter.Eq(c => c.ProjectId, objectId))
                .SortBy(c => c.LoggedAt)
                .ThenBy(c => c.Id)
                .Limit(MaxContributionsPerProject)
                .ToListAsync(cancellationToken);
        }
    }
}
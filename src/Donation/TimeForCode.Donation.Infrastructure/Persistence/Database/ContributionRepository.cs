using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class ContributionRepository : IContributionRepository
    {
        /// <summary>
        /// Upper bound for one project listing; keeps the unpaged endpoint from reading an unbounded collection. The newest contributions win.
        /// </summary>
        private const int MaxContributionsPerProject = 1000;

        /// <summary>
        /// Upper bound for one organization listing, so the unpaged endpoint cannot read an unbounded collection. The earliest contributions win.
        /// </summary>
        private const int MaxContributionsPerOrganization = 10000;

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
                var contributorIndexModel = new CreateIndexModel<Contribution>(
                    Builders<Contribution>.IndexKeys
                        .Ascending(c => c.ContributorId)
                        .Ascending(c => c.LoggedAt),
                    new CreateIndexOptions { Name = "ix_contributions_contributor_logged_at" });

                collection.Indexes.CreateMany([indexModel, contributorIndexModel]);
                _indexesCreated = true;
            }
        }

        public async Task CreateAsync(Contribution contribution)
        {
            await _collection.InsertOneAsync(contribution);
        }

        public async Task<Contribution?> GetByIdAsync(string projectId, string contributionId, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(projectId, out var projectObjectId) || !ObjectId.TryParse(contributionId, out var contributionObjectId))
            {
                return null;
            }

            return await _collection
                .Find(ContributionDefinitions.LiveContributionFilter(projectObjectId, contributionObjectId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(Contribution contribution, CancellationToken cancellationToken)
        {
            var result = await _collection.UpdateOneAsync(ContributionDefinitions.VersionFilter(contribution), ContributionDefinitions.Changes(contribution), cancellationToken: cancellationToken);
            if (result.MatchedCount == 0)
            {
                throw new RepositoryConflictException($"Contribution {contribution.Id} was changed by someone else.");
            }
        }

        public async Task AnonymiseByContributorIdAsync(ObjectId contributorId, CancellationToken cancellationToken)
        {
            var update = Builders<Contribution>.Update
                .Set(c => c.ContributorGitHubLogin, Contributor.DeletedUserLogin)
                .Set(c => c.ContributorAvatarUrl, Contributor.DeletedUserAvatarUrl);

            await _collection.UpdateManyAsync(Builders<Contribution>.Filter.Eq(c => c.ContributorId, contributorId), update, cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<Contribution>> GetByContributorIdsAsync(IReadOnlyCollection<ObjectId> contributorIds, CancellationToken cancellationToken)
        {
            return await _collection
                .Find(ContributionDefinitions.ContributorsFilter(contributorIds))
                .SortBy(c => c.LoggedAt)
                .ThenBy(c => c.Id)
                .Limit(MaxContributionsPerOrganization)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Contribution>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(projectId, out var objectId))
            {
                return [];
            }

            var newest = await _collection
                .Find(ContributionDefinitions.ProjectFilter(objectId))
                .SortByDescending(c => c.LoggedAt)
                .ThenByDescending(c => c.Id)
                .Limit(MaxContributionsPerProject)
                .ToListAsync(cancellationToken);

            newest.Reverse();
            return newest;
        }
    }
}
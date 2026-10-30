using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class ContributorRepository : IContributorRepository
    {
        /// <summary>
        /// Upper bound for the contributors of one donor organization, so the listing and the contribution lookup built on it stay bounded.
        /// </summary>
        private const int MaxContributorsPerOrganization = 1000;

        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<Contributor> _collection;

        public ContributorRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<Contributor>();
            EnsureIndexes(_collection);
        }

        private static void EnsureIndexes(IMongoCollection<Contributor> collection)
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

                var indexModel = new CreateIndexModel<Contributor>(
                    Builders<Contributor>.IndexKeys.Ascending(c => c.PlatformUserId),
                    new CreateIndexOptions<Contributor>
                    {
                        Name = "ux_contributors_platform_user_id",
                        Unique = true
                    });

                var organizationIndexModel = new CreateIndexModel<Contributor>(
                    Builders<Contributor>.IndexKeys.Ascending(c => c.DonorOrganizationId),
                    new CreateIndexOptions<Contributor> { Name = "ix_contributors_donor_organization_id" });

                collection.Indexes.CreateMany([indexModel, organizationIndexModel]);
                _indexesCreated = true;
            }
        }

        public async Task<Contributor> UpsertAsync(Contributor contributor)
        {
            try
            {
                return await UpsertOnceAsync(contributor);
            }
            catch (Exception ex) when (IsDuplicateKey(ex))
            {
                // Two first-time requests of the same user raced on the unique index; the document now exists.
                return await UpsertOnceAsync(contributor);
            }
        }

        private static bool IsDuplicateKey(Exception ex) => ex switch
        {
            MongoCommandException command => command.Code == 11000,
            MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey,
            _ => false
        };

        public async Task<Contributor?> GetByPlatformUserIdAsync(string platformUserId)
        {
            return await _collection
                .Find(Builders<Contributor>.Filter.Eq(c => c.PlatformUserId, platformUserId))
                .FirstOrDefaultAsync();
        }

        public async Task<Contributor?> GetByIdAsync(ObjectId contributorId)
        {
            return await _collection
                .Find(Builders<Contributor>.Filter.Eq(c => c.Id, contributorId))
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<Contributor>> GetByDonorOrganizationIdAsync(ObjectId donorOrganizationId, CancellationToken cancellationToken)
        {
            return await _collection
                .Find(Builders<Contributor>.Filter.Eq(c => c.DonorOrganizationId, donorOrganizationId))
                .SortBy(c => c.GithubLogin)
                .ThenBy(c => c.Id)
                .Limit(MaxContributorsPerOrganization)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateProfileAsync(Contributor contributor, CancellationToken cancellationToken)
        {
            var update = Builders<Contributor>.Update
                .Set(c => c.Role, contributor.Role)
                .Set(c => c.Skills, contributor.Skills)
                .Set(c => c.HoursAvailable, contributor.HoursAvailable)
                .Set(c => c.DonorOrganizationId, contributor.DonorOrganizationId);

            await _collection.UpdateOneAsync(Builders<Contributor>.Filter.Eq(c => c.Id, contributor.Id), update, cancellationToken: cancellationToken);
        }

        public async Task AnonymiseAsync(ObjectId contributorId, CancellationToken cancellationToken)
        {
            var update = Builders<Contributor>.Update
                .Set(c => c.PlatformUserId, $"deleted-{contributorId}")
                .Set(c => c.GithubLogin, Contributor.DeletedUserLogin)
                .Set(c => c.AvatarUrl, Contributor.DeletedUserAvatarUrl);

            await _collection.UpdateOneAsync(Builders<Contributor>.Filter.Eq(c => c.Id, contributorId), update, cancellationToken: cancellationToken);
        }

        private async Task<Contributor> UpsertOnceAsync(Contributor contributor)
        {
            var filter = Builders<Contributor>.Filter.Eq(c => c.PlatformUserId, contributor.PlatformUserId);
            var update = Builders<Contributor>.Update
                .SetOnInsert("_id", contributor.Id)
                .Set(c => c.GithubLogin, contributor.GithubLogin)
                .Set(c => c.AvatarUrl, contributor.AvatarUrl);
            var options = new FindOneAndUpdateOptions<Contributor>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            return await _collection.FindOneAndUpdateAsync(filter, update, options);
        }
    }
}
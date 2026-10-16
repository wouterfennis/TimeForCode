using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class ContributorRepository : IContributorRepository
    {
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

                collection.Indexes.CreateOne(indexModel);
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
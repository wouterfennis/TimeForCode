using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class MaintainerRepository : IMaintainerRepository
    {
        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<Maintainer> _collection;

        public MaintainerRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<Maintainer>();
            EnsureIndexes(_collection);
        }

        private static void EnsureIndexes(IMongoCollection<Maintainer> collection)
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

                var indexModel = new CreateIndexModel<Maintainer>(
                    Builders<Maintainer>.IndexKeys.Ascending(m => m.PlatformUserId),
                    new CreateIndexOptions<Maintainer>
                    {
                        Name = "ux_maintainers_platform_user_id",
                        Unique = true
                    });

                collection.Indexes.CreateOne(indexModel);
                _indexesCreated = true;
            }
        }

        public async Task<Maintainer> UpsertAsync(Maintainer maintainer)
        {
            try
            {
                return await UpsertOnceAsync(maintainer);
            }
            catch (Exception ex) when (IsDuplicateKey(ex))
            {
                // Two first-time requests of the same user raced on the unique index; the document now exists.
                return await UpsertOnceAsync(maintainer);
            }
        }

        public async Task<Maintainer?> GetByPlatformUserIdAsync(string platformUserId)
        {
            return await _collection
                .Find(Builders<Maintainer>.Filter.Eq(m => m.PlatformUserId, platformUserId))
                .FirstOrDefaultAsync();
        }

        private static bool IsDuplicateKey(Exception ex) => ex switch
        {
            MongoCommandException command => command.Code == 11000,
            MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey,
            _ => false
        };

        private async Task<Maintainer> UpsertOnceAsync(Maintainer maintainer)
        {
            var filter = Builders<Maintainer>.Filter.Eq(m => m.PlatformUserId, maintainer.PlatformUserId);
            var update = Builders<Maintainer>.Update
                .SetOnInsert("_id", maintainer.Id)
                .Set(m => m.GithubLogin, maintainer.GithubLogin)
                .Set(m => m.AvatarUrl, maintainer.AvatarUrl);
            var options = new FindOneAndUpdateOptions<Maintainer>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            return await _collection.FindOneAndUpdateAsync(filter, update, options);
        }
    }
}
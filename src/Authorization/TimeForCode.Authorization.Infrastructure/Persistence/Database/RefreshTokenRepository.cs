using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        internal const string TokenHashIndexName = "ux_refresh_token_hash";
        internal const string UserIdIndexName = "ix_refresh_token_user_id";
        internal const string ExpiryIndexName = "ttl_refresh_token_expires_after";

        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<RefreshToken> _collection;

        public RefreshTokenRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<RefreshToken>();
            EnsureIndexes(_collection);
        }

        internal static IReadOnlyList<CreateIndexModel<RefreshToken>> IndexModels { get; } =
        [
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Name = TokenHashIndexName, Unique = true }),
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.UserId),
                new CreateIndexOptions { Name = UserIdIndexName }),
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.ExpiresAfter),
                new CreateIndexOptions { Name = ExpiryIndexName, ExpireAfter = TimeSpan.Zero })
        ];

        private static void EnsureIndexes(IMongoCollection<RefreshToken> collection)
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

                collection.Indexes.CreateMany(IndexModels);
                _indexesCreated = true;
            }
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _collection.Find(t => t.TokenHash == tokenHash).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(RefreshToken entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        public Task UpdateAsync(RefreshToken existingToken)
        {
            return _collection.ReplaceOneAsync(t => t.Id == existingToken.Id, existingToken);
        }

        public Task DeleteAsync(RefreshToken existingToken)
        {
            return _collection.DeleteOneAsync(t => t.Id == existingToken.Id);
        }

        public Task DeleteByUserIdAsync(string userId)
        {
            return _collection.DeleteManyAsync(t => t.UserId == userId);
        }
    }
}
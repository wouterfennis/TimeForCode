using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class LoginCodeRepository : ILoginCodeRepository
    {
        internal const string HashIndexName = "ux_logincode_hash";
        internal const string ExpiryIndexName = "ttl_logincode_expires_after";

        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<LoginCode> _collection;

        public LoginCodeRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<LoginCode>();
            EnsureIndexes(_collection);
        }

        internal static IReadOnlyList<CreateIndexModel<LoginCode>> IndexModels { get; } =
        [
            new CreateIndexModel<LoginCode>(
                Builders<LoginCode>.IndexKeys.Ascending(c => c.CodeHash),
                new CreateIndexOptions { Name = HashIndexName, Unique = true }),
            new CreateIndexModel<LoginCode>(
                Builders<LoginCode>.IndexKeys.Ascending(c => c.ExpiresAfter),
                new CreateIndexOptions { Name = ExpiryIndexName, ExpireAfter = TimeSpan.Zero })
        ];

        private static void EnsureIndexes(IMongoCollection<LoginCode> collection)
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

        public Task AddAsync(LoginCode loginCode)
        {
            return _collection.InsertOneAsync(loginCode);
        }

        public async Task<LoginCode?> TakeAsync(string codeHash)
        {
            return await _collection.FindOneAndDeleteAsync(c => c.CodeHash == codeHash);
        }
    }
}
using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class StateRepository : IStateRepository
    {
        internal const string KeyIndexName = "ux_state_key";
        internal const string ExpiryIndexName = "ttl_state_expires_after";

        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<StateEntry> _collection;

        public StateRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<StateEntry>();
            EnsureIndexes(_collection);
        }

        internal static IReadOnlyList<CreateIndexModel<StateEntry>> IndexModels { get; } =
        [
            new CreateIndexModel<StateEntry>(
                Builders<StateEntry>.IndexKeys.Ascending(s => s.Key),
                new CreateIndexOptions { Name = KeyIndexName, Unique = true }),
            new CreateIndexModel<StateEntry>(
                Builders<StateEntry>.IndexKeys.Ascending(s => s.ExpiresAfter),
                new CreateIndexOptions { Name = ExpiryIndexName, ExpireAfter = TimeSpan.Zero })
        ];

        private static void EnsureIndexes(IMongoCollection<StateEntry> collection)
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

        public Task AddAsync(StateEntry stateEntry)
        {
            return _collection.InsertOneAsync(stateEntry);
        }

        public async Task<StateEntry?> TakeAsync(string key)
        {
            return await _collection.FindOneAndDeleteAsync(s => s.Key == key);
        }
    }
}
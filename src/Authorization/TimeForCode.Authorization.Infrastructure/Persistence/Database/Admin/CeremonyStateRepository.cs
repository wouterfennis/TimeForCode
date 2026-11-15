using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database.Admin
{
    public class CeremonyStateRepository : ICeremonyStateRepository
    {
        internal const string HashIndexName = "ux_ceremonystate_idhash";
        internal const string ExpiryIndexName = "ttl_ceremonystate_expires_after";

        private static readonly object IndexCreationLock = new();
        private static bool _indexesCreated;
        private readonly IMongoCollection<CeremonyState> _collection;

        public CeremonyStateRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<CeremonyState>();
            EnsureIndexes(_collection);
        }

        internal static IReadOnlyList<CreateIndexModel<CeremonyState>> IndexModels { get; } =
        [
            new CreateIndexModel<CeremonyState>(
                Builders<CeremonyState>.IndexKeys.Ascending(c => c.IdHash),
                new CreateIndexOptions { Name = HashIndexName, Unique = true }),
            new CreateIndexModel<CeremonyState>(
                Builders<CeremonyState>.IndexKeys.Ascending(c => c.ExpiresAfter),
                new CreateIndexOptions { Name = ExpiryIndexName, ExpireAfter = TimeSpan.Zero })
        ];

        private static void EnsureIndexes(IMongoCollection<CeremonyState> collection)
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

        public Task AddAsync(CeremonyState state) => _collection.InsertOneAsync(state);

        public async Task<CeremonyState?> TakeAsync(string idHash) =>
            await _collection.FindOneAndDeleteAsync(c => c.IdHash == idHash);
    }
}
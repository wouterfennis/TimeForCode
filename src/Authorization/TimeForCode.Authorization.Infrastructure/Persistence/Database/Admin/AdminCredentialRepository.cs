using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Persistence.Database;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database.Admin
{
    /// <summary>
    /// Own MongoDB collection, no shared repository base class. First-claim semantics rely on the fixed
    /// <see cref="AdminCredential.SingletonId"/> plus MongoDB's unique _id constraint, not application locking.
    /// </summary>
    public class AdminCredentialRepository : IAdminCredentialRepository
    {
        private readonly IMongoCollection<AdminCredential> _collection;

        public AdminCredentialRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<AdminCredential>();
        }

        public Task<AdminCredential?> GetAsync()
        {
            return _collection.Find(Builders<AdminCredential>.Filter.Eq(x => x.Id, AdminCredential.SingletonId)).FirstOrDefaultAsync()!;
        }

        public async Task<bool> TryClaimAsync(AdminCredential credential)
        {
            try
            {
                await _collection.InsertOneAsync(credential);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        // One conditional update, so two assertions that race on the same stored counter cannot both advance it.
        internal static FilterDefinition<AdminCredential> AdvanceFilter(long presented)
        {
            var isSingleton = Builders<AdminCredential>.Filter.Eq(x => x.Id, AdminCredential.SingletonId);
            var counter = presented == 0
                ? Builders<AdminCredential>.Filter.Eq(x => x.SignCount, 0)
                : Builders<AdminCredential>.Filter.Lt(x => x.SignCount, presented);
            return isSingleton & counter;
        }

        public async Task<bool> TryAdvanceSignCountAsync(long presented)
        {
            var result = await _collection.UpdateOneAsync(AdvanceFilter(presented), Builders<AdminCredential>.Update.Set(x => x.SignCount, presented));
            return result.MatchedCount == 1;
        }
    }
}
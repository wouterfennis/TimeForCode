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

        public Task UpdateSignCountAsync(long signCount)
        {
            var update = Builders<AdminCredential>.Update.Set(x => x.SignCount, signCount);
            return _collection.UpdateOneAsync(Builders<AdminCredential>.Filter.Eq(x => x.Id, AdminCredential.SingletonId), update);
        }
    }
}
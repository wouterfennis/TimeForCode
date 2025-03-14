using MongoDB.Bson;
using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class AccountInformationRepository : IAccountInformationRepository
    {
        private readonly IMongoCollection<AccountInformation> _collection;

        public AccountInformationRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<AccountInformation>();
        }

        public async Task<AccountInformation> GetByIdentityProviderIdAsync(string identityProviderId)
        {
            return await _collection.Find(Builders<AccountInformation>.Filter.Eq("IdentityProviderId", identityProviderId)).FirstOrDefaultAsync();
        }

        public async Task<AccountInformation> GetByInternalIdAsync(string internalId)
        {
            return await _collection.Find(Builders<AccountInformation>.Filter.Eq("_id", new ObjectId(internalId))).FirstOrDefaultAsync();
        }

        public async Task<AccountInformation> CreateOrUpdateAsync(AccountInformation entity)
        {
            var existingEntity = await GetByIdentityProviderIdAsync(entity.IdentityProviderId);
            if (existingEntity == null)
            {
                await CreateAsync(entity);
                return entity;
            }
            else
            {
                await UpdateAsync(entity);
                return existingEntity;
            }
        }

        private async Task CreateAsync(AccountInformation entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        private async Task UpdateAsync(AccountInformation entity)
        {
            await _collection.ReplaceOneAsync(Builders<AccountInformation>.Filter.Eq("_id", (entity as dynamic).Id), entity);
        }
    }
}
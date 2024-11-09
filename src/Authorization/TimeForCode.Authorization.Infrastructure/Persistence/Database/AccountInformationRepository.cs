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

        public async Task<AccountInformation> GetByIdAsync(string id)
        {
            return await _collection.Find(Builders<AccountInformation>.Filter.Eq("IdentityProviderId", id)).FirstOrDefaultAsync();
        }

        public async Task CreateOrUpdateAsync(AccountInformation entity)
        {
            var existingEntity = await GetByIdAsync(entity.IdentityProviderId);
            if (existingEntity == null)
            {
                await CreateAsync(entity);
            }
            else
            {
                await UpdateAsync(entity);
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
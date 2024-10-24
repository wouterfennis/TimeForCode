using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class AccountInformationRepository : IRepository<AccountInformation>
    {
        private readonly IMongoCollection<AccountInformation> _collection;

        public AccountInformationRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<AccountInformation>();
        }

        public async Task<IEnumerable<AccountInformation>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<AccountInformation> GetByIdAsync(string identityProviderId)
        {
            return await _collection.Find(Builders<AccountInformation>.Filter.Eq("IdentityProviderId", identityProviderId)).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(AccountInformation entity)
        {
            await _collection.InsertOneAsync(entity);
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

        public async Task UpdateAsync(AccountInformation entity)
        {
            await _collection.ReplaceOneAsync(Builders<AccountInformation>.Filter.Eq("_id", (entity as dynamic).Id), entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _collection.DeleteOneAsync(Builders<AccountInformation>.Filter.Eq("_id", id));
        }
    }
}
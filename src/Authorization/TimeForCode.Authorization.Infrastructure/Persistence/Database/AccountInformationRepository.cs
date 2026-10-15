using MongoDB.Bson;
using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class AccountInformationRepository : IAccountInformationRepository
    {
        private const string LegacyTokenField = "EncryptedGitHubAccessToken";

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

        public async Task<CreateOrUpdateResult> CreateOrUpdateAsync(AccountInformation entity)
        {
            var existingEntity = await GetByIdentityProviderIdAsync(entity.IdentityProviderId);
            if (existingEntity == null)
            {
                await CreateAsync(entity);
                return new CreateOrUpdateResult(entity, IsNewAccount: true);
            }
            else
            {
                await UpdateAsync(entity);
                return new CreateOrUpdateResult(existingEntity, IsNewAccount: false);
            }
        }

        public async Task<long> RemoveStoredGitHubAccessTokensAsync()
        {
            var filter = Builders<AccountInformation>.Filter.Exists(LegacyTokenField);
            var update = Builders<AccountInformation>.Update.Unset(LegacyTokenField);
            var result = await _collection.UpdateManyAsync(filter, update);
            return result.ModifiedCount;
        }

        private async Task CreateAsync(AccountInformation entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        private async Task UpdateAsync(AccountInformation entity)
        {
            var filter = Builders<AccountInformation>.Filter.Eq(x => x.IdentityProviderId, entity.IdentityProviderId);
            var update = Builders<AccountInformation>.Update
                .Set(x => x.Login, entity.Login)
                .Set(x => x.NodeId, entity.NodeId)
                .Set(x => x.AvatarUrl, entity.AvatarUrl)
                .Set(x => x.Name, entity.Name)
                .Set(x => x.Company, entity.Company)
                .Set(x => x.Email, entity.Email)
                .Set(x => x.Bio, entity.Bio)
                .Set(x => x.Location, entity.Location);
            await _collection.UpdateOneAsync(filter, update);
        }
    }
}
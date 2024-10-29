using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IMongoCollection<RefreshToken> _collection;

        public RefreshTokenRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<RefreshToken>();
        }

        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            return await _collection.Find(Builders<RefreshToken>.Filter.Eq("token", token)).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(RefreshToken entity)
        {
            await _collection.InsertOneAsync(entity);
        }
    }
}
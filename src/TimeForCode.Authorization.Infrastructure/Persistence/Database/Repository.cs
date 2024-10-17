using MongoDB.Driver;
using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;

        public Repository(IMongoDbContext context)
        {
            _collection = context.GetCollection<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<T> GetByIdAsync(string id)
        {
            return await _collection.Find(Builders<T>.Filter.Eq("_id", id)).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(T entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(T entity)
        {
            await _collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", (entity as dynamic).Id), entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", id));
        }
    }
}
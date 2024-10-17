using MongoDB.Driver;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public interface IMongoDbContext
    {
        IMongoCollection<T> GetCollection<T>();
    }
}
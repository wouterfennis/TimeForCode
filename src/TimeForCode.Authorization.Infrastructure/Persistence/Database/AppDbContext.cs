using MongoDB.Driver;
using TimeForCode.Authorization.Infrastructure.Options;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class MongoDbContext : IMongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(DbOptions dbOptions)
        {
            var client = new MongoClient(dbOptions.ConnectionString);
            _database = client.GetDatabase(dbOptions.DatabaseName);
        }

        public IMongoCollection<T> GetCollection<T>()
        {
            return _database.GetCollection<T>(nameof(T));
        }
    }
}

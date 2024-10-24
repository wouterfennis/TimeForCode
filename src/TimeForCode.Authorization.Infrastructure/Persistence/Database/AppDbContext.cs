using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Infrastructure.Options;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    public class MongoDbContext : IMongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<DbOptions> dbOptions)
        {
            var client = new MongoClient(dbOptions.Value.ConnectionString);
            _database = client.GetDatabase(dbOptions.Value.DatabaseName);
        }

        public IMongoCollection<T> GetCollection<T>() where T : DocumentEntity
        {
            return _database.GetCollection<T>(typeof(T).Name);
        }
    }
}
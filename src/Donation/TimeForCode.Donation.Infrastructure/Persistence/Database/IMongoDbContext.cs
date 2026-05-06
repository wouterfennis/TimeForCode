using MongoDB.Driver;
using TimeForCode.Donation.Domain.Entities;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    public interface IMongoDbContext
    {
        IMongoCollection<T> GetCollection<T>() where T : DocumentEntity;
    }
}

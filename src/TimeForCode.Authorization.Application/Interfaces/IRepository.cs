using MongoDB.Bson;
using TimeForCode.Authorization.Domain;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IRepository<T> where T : DocumentEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task CreateAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(string id);
        Task CreateOrUpdateAsync(T entity);
    }
}
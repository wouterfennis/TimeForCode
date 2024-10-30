using MongoDB.Bson;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task CreateAsync(RefreshToken entity);
        Task UpdateAsync(RefreshToken existingToken);
    }
}
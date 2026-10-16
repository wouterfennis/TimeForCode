using MongoDB.Bson;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task CreateAsync(RefreshToken entity);

        /// <summary>
        /// Atomically marks the token as rotated when it has not been rotated yet.
        /// </summary>
        /// <returns><c>true</c> when this call performed the rotation; <c>false</c> when it was already rotated or is gone.</returns>
        Task<bool> TryMarkRotatedAsync(ObjectId id, DateTimeOffset rotatedAt);
        Task DeleteAsync(RefreshToken existingToken);
        Task DeleteByUserIdAsync(string userId);
    }
}
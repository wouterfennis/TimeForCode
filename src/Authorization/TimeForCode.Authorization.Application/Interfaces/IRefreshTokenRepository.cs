using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task CreateAsync(RefreshToken entity);
        Task UpdateAsync(RefreshToken existingToken);
        Task DeleteAsync(RefreshToken existingToken);
        Task DeleteByUserIdAsync(string userId);
    }
}
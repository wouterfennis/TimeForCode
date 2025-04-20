using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> CreateRefreshTokenAsync(string userId);
        Task<Result<RefreshToken>> ReplaceRefreshTokenAsync(RefreshToken oldRefreshToken);
        Task ExpireRefreshTokenAsync(RefreshToken refreshToken);
    }
}
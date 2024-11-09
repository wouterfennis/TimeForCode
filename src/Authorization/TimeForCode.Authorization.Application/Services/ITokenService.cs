using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    public interface ITokenService
    {
        Task<Result<ExternalAccessToken>> GetAccessTokenFromExternalProviderAsync(string state, string code);
        AccessToken GenerateInternalToken(string userId);
        Task<RefreshToken> CreateRefreshTokenAsync(string userId);
        Task<Result<AccessToken>> RefreshInternalTokenAsync(RefreshToken refreshToken);
        Task<Result<RefreshToken>> ReplaceRefreshTokenAsync(RefreshToken oldRefreshToken);
        Task ExpireRefreshTokenAsync(RefreshToken refreshToken);
    }
}
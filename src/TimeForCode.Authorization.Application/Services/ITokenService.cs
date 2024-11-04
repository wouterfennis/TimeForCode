using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    public interface ITokenService
    {
        Task<Result<ExternalAccessToken>> GetAccessTokenFromExternalProvider(string state, string code);
        AccessToken GenerateInternalToken(string userId);
        Task<RefreshToken> CreateRefreshToken(string userId);
        Task<Result<AccessToken>> RefreshInternalTokenAsync(RefreshToken refreshToken);
        Task<Result<RefreshToken>> ReplaceRefreshToken(RefreshToken oldRefreshToken);
    }
}
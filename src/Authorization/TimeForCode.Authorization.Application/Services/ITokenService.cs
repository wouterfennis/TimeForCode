using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Services
{
    public interface ITokenService
    {
        Task<Result<ExternalAccessToken>> GetAccessTokenFromExternalProviderAsync(string state, string code);
        AccessToken GenerateInternalToken(string userId);
        Task<Result<AccessToken>> RefreshInternalTokenAsync(RefreshToken refreshToken);
        Uri GetRedirectUri(string state);
    }
}
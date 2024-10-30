using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Services
{
    public interface ITokenService
    {
        Task<Result<AccessToken>> GetAccessTokenFromExternalProvider(string state, string code);
        AccessToken GenerateInternalToken(string userId);
        Task<RefreshToken> CreateAndReplaceRefreshToken(RefreshToken? oldRefreshToken);
    }
}
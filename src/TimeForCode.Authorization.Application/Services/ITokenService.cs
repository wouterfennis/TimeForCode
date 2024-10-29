using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Services
{
    public interface ITokenService
    {
        Task<Result<string>> GetAccessTokenFromExternalProvider(string state, string code);
        string GenerateInternalToken(string userId);
    }
}
using Microsoft.IdentityModel.Tokens;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IIdentityProviderService
    {
        Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code);
        Task<Result<AccountInformation>> GetAccountInformation(ExternalAccessToken accessToken);
        Task<TokenValidationParameters> GetTokenValidationParameters();
    }
}
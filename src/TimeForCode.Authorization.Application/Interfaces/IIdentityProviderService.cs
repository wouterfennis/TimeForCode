using Microsoft.IdentityModel.Tokens;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IIdentityProviderService
    {
        Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code);
        Task<Result<AccountInformation>> GetAccountInformation(AccessToken model);
        Task<TokenValidationParameters> GetTokenValidationParameters();
    }
}
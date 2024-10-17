using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IIdentityProviderService
    {
        public Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code);
        public Task<Result<AccountInformation>> GetAccountInformation(GetAccountInformationModel model);
    }
}
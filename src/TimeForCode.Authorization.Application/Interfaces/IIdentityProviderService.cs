using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IIdentityProviderService
    {
        public Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code);
        public Task<Result<AccountInformation>> GetAccountInformation(GetAccountInformationModel model);
    }
}

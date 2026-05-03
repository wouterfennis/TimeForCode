using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    public interface IAccountService
    {
        Task<Result<SaveAccountResult>> SaveAccountInformation(string state, ExternalAccessToken accessToken);
    }
}
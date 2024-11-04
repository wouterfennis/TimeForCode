using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    public interface IAccountService
    {
        Task<Result<AccountInformation>> SaveAccountInformation(string state, ExternalAccessToken accessToken);
    }
}
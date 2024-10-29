using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Services
{
    public interface IAccountService
    {
        Task<Result<AccountInformation>> SaveAccountInformation(string state, string accessToken);
    }
}
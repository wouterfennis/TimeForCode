using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IAccountInformationRepository
    {
        Task<AccountInformation> GetByIdAsync(string id);
        Task CreateOrUpdateAsync(AccountInformation entity);
    }
}
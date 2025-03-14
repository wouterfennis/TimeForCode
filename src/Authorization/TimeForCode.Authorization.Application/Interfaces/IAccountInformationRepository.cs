using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IAccountInformationRepository
    {
        Task<AccountInformation> GetByIdentityProviderIdAsync(string identityProviderId);
        Task<AccountInformation> GetByInternalIdAsync(string internalId);
        Task<AccountInformation> CreateOrUpdateAsync(AccountInformation entity);
    }
}
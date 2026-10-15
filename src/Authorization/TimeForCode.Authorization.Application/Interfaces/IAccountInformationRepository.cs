using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IAccountInformationRepository
    {
        Task<AccountInformation> GetByIdentityProviderIdAsync(string identityProviderId);
        Task<AccountInformation> GetByInternalIdAsync(string internalId);
        Task<CreateOrUpdateResult> CreateOrUpdateAsync(AccountInformation entity);

        /// <summary>
        /// Removes the GitHub access token that earlier versions stored on accounts.
        /// </summary>
        /// <returns>The number of accounts that were cleaned.</returns>
        Task<long> RemoveStoredGitHubAccessTokensAsync();
    }
}
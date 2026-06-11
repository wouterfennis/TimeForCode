using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IIdentityProviderServiceFactory _identityProviderServiceFactory;
        private readonly IAccountInformationRepository _accountRepository;
        private readonly IEncryptionService _encryptionService;

        public AccountService(
            IIdentityProviderServiceFactory identityProviderServiceFactory,
            IAccountInformationRepository accountRepository,
            IEncryptionService encryptionService)
        {
            _identityProviderServiceFactory = identityProviderServiceFactory;
            _accountRepository = accountRepository;
            _encryptionService = encryptionService;
        }

        public async Task<Result<SaveAccountResult>> SaveAccountInformation(string state, ExternalAccessToken accessToken)
        {
            var identityProviderServiceResult = _identityProviderServiceFactory.GetIdentityProviderServiceFromState(state);

            if (identityProviderServiceResult.IsFailure)
            {
                return Result<SaveAccountResult>.Failure(identityProviderServiceResult.ErrorMessage);
            }

            var accountInformationResult = await identityProviderServiceResult.Value.GetAccountInformation(accessToken);

            if (accountInformationResult.IsFailure)
            {
                return Result<SaveAccountResult>.Failure(accountInformationResult.ErrorMessage);
            }

            var accountInformation = accountInformationResult.Value;
            accountInformation.EncryptedGitHubAccessToken = _encryptionService.Encrypt(accessToken.Token);

            var createOrUpdateResult = await _accountRepository.CreateOrUpdateAsync(accountInformation);

            return Result<SaveAccountResult>.Success(new SaveAccountResult(createOrUpdateResult.AccountInformation, createOrUpdateResult.IsNewAccount));
        }
    }
}
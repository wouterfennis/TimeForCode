using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            IIdentityProviderServiceFactory identityProviderServiceFactory,
            IAccountInformationRepository accountRepository,
            IEncryptionService encryptionService,
            ILogger<AccountService> logger)
        {
            _identityProviderServiceFactory = identityProviderServiceFactory;
            _accountRepository = accountRepository;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task<Result<SaveAccountResult>> SaveAccountInformation(string state, ExternalAccessToken accessToken)
        {
            _logger.LogInformation("Saving account information for state {State}", state);

            var identityProviderServiceResult = _identityProviderServiceFactory.GetIdentityProviderServiceFromState(state);

            if (identityProviderServiceResult.IsFailure)
            {
                _logger.LogWarning("Failed to get identity provider service from state: {Error}", identityProviderServiceResult.ErrorMessage);
                return Result<SaveAccountResult>.Failure(identityProviderServiceResult.ErrorMessage);
            }

            var accountInformationResult = await identityProviderServiceResult.Value.GetAccountInformation(accessToken);

            if (accountInformationResult.IsFailure)
            {
                _logger.LogWarning("Failed to get account information from identity provider: {Error}", accountInformationResult.ErrorMessage);
                return Result<SaveAccountResult>.Failure(accountInformationResult.ErrorMessage);
            }

            var accountInformation = accountInformationResult.Value;
            accountInformation.EncryptedGitHubAccessToken = _encryptionService.Encrypt(accessToken.Token);

            var createOrUpdateResult = await _accountRepository.CreateOrUpdateAsync(accountInformation);

            _logger.LogInformation("Account information saved for user {UserId}", createOrUpdateResult.AccountInformation.Id);

            return Result<SaveAccountResult>.Success(new SaveAccountResult(createOrUpdateResult.AccountInformation, createOrUpdateResult.IsNewAccount));
        }
    }
}
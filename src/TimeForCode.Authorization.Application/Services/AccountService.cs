using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IIdentityProviderServiceFactory _identityProviderServiceFactory;
        private readonly IAccountInformationRepository _accountRepository;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            IIdentityProviderServiceFactory identityProviderServiceFactory,
            IAccountInformationRepository accountRepository,
            ILogger<AccountService> logger)
        {
            _identityProviderServiceFactory = identityProviderServiceFactory;
            _accountRepository = accountRepository;
            _logger = logger;
        }

        public async Task<Result<AccountInformation>> SaveAccountInformation(string state, AccessToken accessToken)
        {
            var identityProviderServiceResult = _identityProviderServiceFactory.GetIdentityProviderServiceFromState(state);

            if (identityProviderServiceResult.IsFailure)
            {
                return Result<AccountInformation>.Failure(identityProviderServiceResult.ErrorMessage);
            }

            var accountInformationResult = await identityProviderServiceResult.Value.GetAccountInformation(accessToken);

            if (accountInformationResult.IsFailure)
            {
                return Result<AccountInformation>.Failure(accountInformationResult.ErrorMessage);
            }

            await _accountRepository.CreateOrUpdateAsync(accountInformationResult.Value);

            _logger.LogDebug(accountInformationResult.Value.ToString());
            return Result<AccountInformation>.Success(accountInformationResult.Value);
        }
    }
}

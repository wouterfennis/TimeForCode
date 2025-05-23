﻿using Microsoft.Extensions.Logging;
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

        public async Task<Result<AccountInformation>> SaveAccountInformation(string state, ExternalAccessToken accessToken)
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

            var accountInformation = await _accountRepository.CreateOrUpdateAsync(accountInformationResult.Value);

            _logger.LogDebug("Account information: {AccountInformation}", accountInformation);
            return Result<AccountInformation>.Success(accountInformation);
        }
    }
}
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class CallbackHandler : IRequestHandler<CallbackCommand, Result<CallbackResult>>
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IIdentityProviderServiceFactory _identityProviderServiceFactory;
        private readonly ILogger<CallbackHandler> _logger;
        private readonly IRepository<AccountInformation> _accountRepository;

        public CallbackHandler(IOptions<ExternalIdentityProviderOptions> options,
            IMemoryCache memoryCache,
            IIdentityProviderServiceFactory identityProviderServiceFactory,
            ILogger<CallbackHandler> logger,
            IRepository<AccountInformation> accountRepository)
        {
            _memoryCache = memoryCache;
            _identityProviderServiceFactory = identityProviderServiceFactory;
            _logger = logger;
            _accountRepository = accountRepository;
        }

        public async Task<Result<CallbackResult>> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            var result = GetIdentityProviderService(request.State);
            if (result.IsFailure)
            {
                return Result<CallbackResult>.Failure(result.ErrorMessage);
            }
            var identityProviderService = result.Value;

            var externalAccessTokenResult = await identityProviderService.GetAccessTokenAsync(request.Code);
            if (externalAccessTokenResult.IsFailure)
            {
                return Result<CallbackResult>.Failure(externalAccessTokenResult.ErrorMessage);
            }

            var saveResult = await SaveAccountInformation(identityProviderService, externalAccessTokenResult);

            if (saveResult.IsFailure)
            {
                return Result<CallbackResult>.Failure(saveResult.ErrorMessage);
            }

            //TODO: exchange for internal access token

            return Result<CallbackResult>.Success(new CallbackResult
            {
                InternalAccessToken = externalAccessTokenResult.Value.AccessToken
            });
        }

        private async Task<Result<AccountInformation>> SaveAccountInformation(IIdentityProviderService identityProviderService, Result<GetAccessTokenResult> externalAccessTokenResult)
        {
            var getAccountInformationModel = new GetAccountInformationModel
            {
                AccessToken = externalAccessTokenResult.Value.AccessToken
            };
            var accountInformationResult = await identityProviderService.GetAccountInformation(getAccountInformationModel);

            if (accountInformationResult.IsFailure)
            {
                return Result<AccountInformation>.Failure(accountInformationResult.ErrorMessage);
            }

            await _accountRepository.CreateAsync(accountInformationResult.Value);

            _logger.LogDebug(accountInformationResult.Value.ToString());
            return Result<AccountInformation>.Success(accountInformationResult.Value);
        }

        private Result<IIdentityProviderService> GetIdentityProviderService(string state)
        {
            if (!_memoryCache.TryGetValue(state, out IdentityProvider identityProvider))
            {
                return Result<IIdentityProviderService>.Failure("State is not known");
            }

            return _identityProviderServiceFactory.GetIdentityProviderService(identityProvider);
        }

    }
}
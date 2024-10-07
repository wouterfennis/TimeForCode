using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class CallbackHandler : IRequestHandler<CallbackCommand, Result<CallbackResult>>
    {
        private readonly ExternalIdentityProviderOptions _options;
        private readonly IMemoryCache _memoryCache;
        private readonly IIdentityProviderServiceFactory _identityProviderServiceFactory;

        public CallbackHandler(IOptions<ExternalIdentityProviderOptions> options, 
            IMemoryCache memoryCache,
            IIdentityProviderServiceFactory identityProviderServiceFactory)
        {
            _options = options.Value;
            _memoryCache = memoryCache;
            _identityProviderServiceFactory = identityProviderServiceFactory;
        }

        public async Task<Result<CallbackResult>> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            var result = GetIdentityProviderService(request.State);
            if (result.IsFailure)
            {
                Result<CallbackResult>.Failure(result.ErrorMessage);
            }
            var identityProviderService = result.Data;

            var externalAccessTokenResult = await identityProviderService.GetAccessTokenAsync(request.Code);
            if(externalAccessTokenResult.IsFailure)
            {
                Result<CallbackResult>.Failure(externalAccessTokenResult.ErrorMessage);
            }

            // save account information
            var getAccountInformationModel = new GetAccountInformationModel
            {
                AccessToken = externalAccessTokenResult.Data.AccessToken
            };
            var accountInformation = await identityProviderService.GetAccountInformation(getAccountInformationModel);

            // exchange for internal access token

            return Result<CallbackResult>.Success(new CallbackResult
            {
                InternalAccessToken = externalAccessTokenResult.Data.AccessToken
            });
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

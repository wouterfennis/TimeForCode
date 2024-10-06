using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Web;
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
        private readonly IRestService _restService;

        public CallbackHandler(IOptions<ExternalIdentityProviderOptions> options, 
            IMemoryCache memoryCache,
            IRestService restService)
        {
            _options = options.Value;
            _memoryCache = memoryCache;
            _restService = restService;
        }

        public async Task<Result<CallbackResult>> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            var result = GetIdentityProvider(request.State);
            if (result.IsFailure)
            {
                Result<CallbackResult>.Failure(result.ErrorMessage);
            }

            var model = BuildAccessTokenModel(request.Code, result.Data);
            var externalAccessTokenResult = await _restService.GetAccessTokenAsync(model);
            if(externalAccessTokenResult.IsFailure)
            {
                Result<CallbackResult>.Failure(externalAccessTokenResult.ErrorMessage);
            }

            // save account information

            // exchange for internal access token

            return Result<CallbackResult>.Success(new CallbackResult
            {
                InternalAccessToken = externalAccessTokenResult.Data.AccessToken
            });
        }

        private Result<ExternalIdentityProvider> GetIdentityProvider(string state)
        {
            if (!_memoryCache.TryGetValue(state, out IdentityProvider identityProvider))
            {
                return Result<ExternalIdentityProvider>.Failure("State is not known");
            }

            var options = _options.GetExternalIdentityProvider(identityProvider);
            return Result<ExternalIdentityProvider>.Success(options);
        }

        private GetAccessTokenModel BuildAccessTokenModel(string code, ExternalIdentityProvider identityProvider)
        {
            return new GetAccessTokenModel
            {
                Host = identityProvider.Host,
                ClientId = identityProvider.ClientId,
                ClientSecret = identityProvider.ClientSecret,
                Code = code
            };
        }
    }
}

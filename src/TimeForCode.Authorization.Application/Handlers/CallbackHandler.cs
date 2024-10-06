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
    public class CallbackHandler : IRequestHandler<CallbackCommand, Uri>
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

        public Task<Uri> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            var result = GetIdentityProvider(request.State);
            if (result.IsFailure)
            {
                throw new Exception("State is not known");
            }

            var model = BuildAccessTokenModel(request.Code, result.Data);
            var accessToken = await _restService.GetAccessTokenAsync(model);
        }

        private Result<IdentityProvider> GetIdentityProvider(string state)
        {
            if (!_memoryCache.TryGetValue(state, out IdentityProvider identityProvider))
            {
                return Result<IdentityProvider>.Failure("State is not known");
            }

            return Result<IdentityProvider>.Success(identityProvider);
        }

        private GetAccessTokenModel BuildAccessTokenModel(string code, IdentityProvider identityProvider)
        {
            string clientId = string.Empty;
            string clientSecret = string.Empty;
            switch (identityProvider)
            {
                case IdentityProvider.Github:
                    clientId = _options.Github.ClientId;
                    clientSecret = _options.Github.ClientSecret;
                    break;
                default:
                    break;
            }

            return new GetAccessTokenModel
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                Code = code
            };
        }
    }
}

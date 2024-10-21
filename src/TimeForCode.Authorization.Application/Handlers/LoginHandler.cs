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
    public class LoginHandler : IRequestHandler<LoginCommand, Uri>
    {
        private readonly ExternalIdentityProviderOptions _options;
        private readonly IMemoryCache _memoryCache;
        private readonly IRandomGenerator _randomGenerator;

        public LoginHandler(IOptions<ExternalIdentityProviderOptions> options,
            IMemoryCache memoryCache,
            IRandomGenerator randomGenerator)
        {
            _options = options.Value;
            _memoryCache = memoryCache;
            _randomGenerator = randomGenerator;
        }

        public Task<Uri> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Path = "/login/oauth/authorize"
            };

            string state = CreateState(request.IdentityProvider);
            var identityProvider = _options.GetExternalIdentityProvider(request.IdentityProvider);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["state"] = state;
            query["redirect_uri"] = _options.CallbackUri;
            query["scope"] = "user";
            query["client_id"] = identityProvider.ClientId;

            uriBuilder.Host = identityProvider.Host;
            uriBuilder.Port = identityProvider.HostPort.HasValue ? identityProvider.HostPort.Value : uriBuilder.Port;
            uriBuilder.Query = query.ToString();

            return Task.FromResult(uriBuilder.Uri);
        }

        private string CreateState(IdentityProvider identityProvider)
        {
            string state = _randomGenerator.GenerateRandomString();
            _memoryCache.Set(state, identityProvider, TimeSpan.FromMinutes(10));

            return state;
        }
    }
}
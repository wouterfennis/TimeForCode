using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Web;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
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
            var identityProvider = _options.GetExternalIdentityProvider(request.IdentityProvider);
            string state = CreateState(request.IdentityProvider);

            var uriBuilder = new UriBuilder
            {
                Host = identityProvider.Host,
                Port = identityProvider.HostPort ?? -1,
                Path = OAuthConstants.AuthorizationEndpoint,
                Query = BuildQuery(state, identityProvider.ClientId)
            };

            return Task.FromResult(uriBuilder.Uri);
        }

        private string CreateState(IdentityProvider identityProvider)
        {
            string state = _randomGenerator.GenerateRandomString();
            _memoryCache.Set(state, identityProvider, TimeSpan.FromMinutes(10));
            return state;
        }

        private string BuildQuery(string state, string clientId)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query[OAuthConstants.State] = state;
            query[OAuthConstants.RedirectUri] = _options.CallbackUri;
            query[OAuthConstants.Scope] = OAuthConstants.UserScope;
            query[OAuthConstants.ClientId] = clientId;
            return query.ToString()!;
        }
    }
}
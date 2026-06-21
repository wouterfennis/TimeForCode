using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Web;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class LoginHandler : IRequestHandler<LoginCommand, Uri>
    {
        private readonly ExternalIdentityProviderOptions _options;
        private readonly IStateRepository _stateRepository;
        private readonly IRandomGenerator _randomGenerator;
        private readonly ILogger<LoginHandler> _logger;

        public LoginHandler(IOptions<ExternalIdentityProviderOptions> options,
            IStateRepository stateRepository,
            IRandomGenerator randomGenerator,
            ILogger<LoginHandler> logger)
        {
            _options = options.Value;
            _stateRepository = stateRepository;
            _randomGenerator = randomGenerator;
            _logger = logger;
        }

        public Task<Uri> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling login for identity provider {IdentityProvider}", request.IdentityProvider);

            var identityProvider = _options.GetExternalIdentityProvider(request.IdentityProvider);
            string state = CreateState(request.IdentityProvider, request.RedirectUri);

            var uriBuilder = new UriBuilder
            {
                Host = identityProvider.LoginHost,
                Path = OAuthConstants.AuthorizationEndpoint,
                Query = BuildQuery(state, identityProvider.ClientId)
            };

            uriBuilder.Port = identityProvider.LoginHostPort ?? uriBuilder.Port;

            _logger.LogDebug("Login redirect URI built for identity provider {IdentityProvider}", request.IdentityProvider);

            return Task.FromResult(uriBuilder.Uri);
        }

        private string CreateState(IdentityProvider identityProvider, Uri redirectUri)
        {
            string stateKey = _randomGenerator.GenerateRandomString();
            StateEntry stateEntry = new StateEntry(stateKey, identityProvider, redirectUri);
            _stateRepository.AddState(stateEntry);
            return stateKey;
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
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using System.Web;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
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
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<LoginHandler> _logger;

        public LoginHandler(IOptions<ExternalIdentityProviderOptions> options,
            IStateRepository stateRepository,
            IRandomGenerator randomGenerator,
            TimeProvider timeProvider,
            ILogger<LoginHandler> logger)
        {
            _options = options.Value;
            _stateRepository = stateRepository;
            _randomGenerator = randomGenerator;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Uri> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling login for identity provider {IdentityProvider}", request.IdentityProvider);

            var identityProvider = _options.GetExternalIdentityProvider(request.IdentityProvider);
            StateEntry stateEntry = await CreateStateAsync(request.IdentityProvider, request.RedirectUri);

            var uriBuilder = new UriBuilder
            {
                Host = identityProvider.LoginHost,
                Path = OAuthConstants.AuthorizationEndpoint,
                Query = BuildQuery(stateEntry, identityProvider.ClientId)
            };

            uriBuilder.Port = identityProvider.LoginHostPort ?? uriBuilder.Port;

            _logger.LogDebug("Login redirect URI built for identity provider {IdentityProvider}", request.IdentityProvider);

            return uriBuilder.Uri;
        }

        private async Task<StateEntry> CreateStateAsync(IdentityProvider identityProvider, Uri redirectUri)
        {
            var stateEntry = new StateEntry
            {
                Id = ObjectId.GenerateNewId(),
                Key = _randomGenerator.GenerateRandomString(),
                IdentityProvider = identityProvider,
                RedirectUri = redirectUri,
                CodeVerifier = _randomGenerator.GenerateCodeVerifier(),
                ExpiresAfter = _timeProvider.GetUtcNow().Add(StateEntry.Lifetime)
            };
            await _stateRepository.AddAsync(stateEntry);
            return stateEntry;
        }

        private string BuildQuery(StateEntry stateEntry, string clientId)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query[OAuthConstants.State] = stateEntry.Key;
            query[OAuthConstants.RedirectUri] = _options.CallbackUri;
            query[OAuthConstants.ClientId] = clientId;
            query[OAuthConstants.CodeChallenge] = Pkce.CreateS256Challenge(stateEntry.CodeVerifier);
            query[OAuthConstants.CodeChallengeMethod] = OAuthConstants.CodeChallengeMethodS256;
            return query.ToString()!;
        }
    }
}
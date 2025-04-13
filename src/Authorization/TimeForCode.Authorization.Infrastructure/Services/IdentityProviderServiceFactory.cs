using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Services.Github;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class IdentityProviderServiceFactory : IIdentityProviderServiceFactory
    {
        private readonly IEnumerable<IIdentityProviderService> _services;
        private readonly IStateRepository _stateRepository;
        private readonly ILogger<IdentityProviderServiceFactory> _logger;

        public IdentityProviderServiceFactory(IEnumerable<IIdentityProviderService> services, 
            IStateRepository stateRepository,
            ILogger<IdentityProviderServiceFactory> logger)
        {
            _services = services;
            _stateRepository = stateRepository;
            _logger = logger;
        }

        public Result<IIdentityProviderService> GetIdentityProviderServiceFromState(string state)
        {
            _logger.LogDebug("GetIdentityProviderServiceFromState called with state: {State}", state);
            var identityProviderResult = GetIdentityProvider(state);

            if (identityProviderResult.IsFailure)
            {
                return Result<IIdentityProviderService>.Failure(identityProviderResult.ErrorMessage);
            }

            switch (identityProviderResult.Value)
            {
                case IdentityProvider.Github:
                    var githubService = _services.Single(x => x.GetType() == typeof(GithubService));
                    return Result<IIdentityProviderService>.Success(githubService);
                default:
                    return Result<IIdentityProviderService>.Failure("Identity provider not supported");
            }
        }

        private Result<IdentityProvider> GetIdentityProvider(string state)
        {
            StateEntry? stateEntry = _stateRepository.GetState(state);
            if (stateEntry == null)
            {
                return Result<IdentityProvider>.Failure("State is not known");
            }

            return Result<IdentityProvider>.Success(stateEntry.IdentityProvider);
        }
    }
}
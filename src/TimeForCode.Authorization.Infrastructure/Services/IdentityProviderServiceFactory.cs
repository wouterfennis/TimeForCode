using Microsoft.Extensions.Caching.Memory;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Infrastructure.Services.Github;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class IdentityProviderServiceFactory : IIdentityProviderServiceFactory
    {
        private readonly IEnumerable<IIdentityProviderService> _services;
        private readonly IMemoryCache _memoryCache;

        public IdentityProviderServiceFactory(IEnumerable<IIdentityProviderService> services, IMemoryCache memoryCache)
        {
            _services = services;
            _memoryCache = memoryCache;
        }

        public Result<IIdentityProviderService> GetIdentityProviderServiceFromState(string state)
        {
            var identityProviderResult = GetIdentityProvider(state);

            if(identityProviderResult.IsFailure)
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
            if (!_memoryCache.TryGetValue(state, out IdentityProvider identityProvider))
            {
                return Result<IdentityProvider>.Failure("State is not known");
            }

            return Result<IdentityProvider>.Success(identityProvider);
        }
    }
}
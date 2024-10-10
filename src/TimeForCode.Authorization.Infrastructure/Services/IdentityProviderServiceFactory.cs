using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class IdentityProviderServiceFactory : IIdentityProviderServiceFactory
    {
        private readonly IEnumerable<IIdentityProviderService> _services;

        public IdentityProviderServiceFactory(IEnumerable<IIdentityProviderService> services)
        {
            _services = services;
        }
        
        public Result<IIdentityProviderService> GetIdentityProviderService(IdentityProvider identityProvider)
        {
            switch (identityProvider)
            {
                case IdentityProvider.Github:
                    var githubService = _services.Single(x => x.GetType() == typeof(GithubService));
                    return Result<IIdentityProviderService>.Success(githubService);
                default:
                    return Result<IIdentityProviderService>.Failure("Identity provider not supported");
            }
        }
    }
}

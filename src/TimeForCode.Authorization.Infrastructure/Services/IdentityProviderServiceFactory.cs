using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class IdentityProviderServiceFactory : IIdentityProviderServiceFactory
    {
        private readonly GithubService _githubService;

        public IdentityProviderServiceFactory(GithubService githubService)
        {
            _githubService = githubService;
        }
        
        public Result<IIdentityProviderService> GetIdentityProviderService(IdentityProvider identityProvider)
        {
            switch (identityProvider)
            {
                case IdentityProvider.Github:
                    return Result<IIdentityProviderService>.Success(_githubService);
                default:
                    return Result<IIdentityProviderService>.Failure("Identity provider not supported");
            }
        }
    }
}

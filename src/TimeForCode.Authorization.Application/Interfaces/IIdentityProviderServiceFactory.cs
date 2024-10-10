using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IIdentityProviderServiceFactory
    {
        public Result<IIdentityProviderService> GetIdentityProviderService(IdentityProvider identityProvider);
    }
}

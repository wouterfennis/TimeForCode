using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IIdentityProviderServiceFactory
    {
        public Result<IIdentityProviderService> GetIdentityProviderServiceFromState(string state);
    }
}
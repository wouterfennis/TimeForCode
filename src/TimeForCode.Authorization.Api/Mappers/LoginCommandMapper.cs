using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Api.Mappers
{
    public static class LoginCommandMapper
    {
        public static LoginCommand MapToCommand(this LoginRequestModel model)
        {
            return new LoginCommand
            {
                IdentityProvider = model.IdentityProvider.MapToValue()
            };
        }

        public static Values.IdentityProvider MapToValue(this IdentityProvider identityProvider)
        {
            switch (identityProvider)
            {
                case IdentityProvider.Github:
                    return Values.IdentityProvider.Github;
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}

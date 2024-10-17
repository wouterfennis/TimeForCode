using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Api.Mappers
{
    /// <summary>
    /// Provides extension methods for mapping LoginRequestModel to LoginCommand.
    /// </summary>
    public static class LoginCommandMapper
    {
        /// <summary>
        /// Maps a <see cref="LoginRequestModel"/> to a <see cref="LoginCommand"/>.
        /// </summary>
        /// <param name="model">The LoginRequestModel to map.</param>
        /// <returns>A LoginCommand with the mapped values.</returns>
        public static LoginCommand MapToCommand(this LoginRequestModel model)
        {
            return new LoginCommand
            {
                IdentityProvider = model.IdentityProvider.MapToValue()
            };
        }

        /// <summary>
        /// Maps an <see cref="IdentityProvider"/> to a <see cref="Values.IdentityProvider"/>.
        /// </summary>
        /// <param name="identityProvider">The IdentityProvider to map.</param>
        /// <returns>A Values.IdentityProvider with the mapped value.</returns>
        /// <exception cref="System.NotImplementedException">Thrown when the IdentityProvider is not implemented.</exception>
        public static Values.IdentityProvider MapToValue(this IdentityProvider identityProvider)
        {
            switch (identityProvider)
            {
                case IdentityProvider.Github:
                    return Values.IdentityProvider.Github;
                default:
                    throw new System.NotImplementedException($"The IdentityProvider {identityProvider} is not implemented.");
            }
        }
    }
}

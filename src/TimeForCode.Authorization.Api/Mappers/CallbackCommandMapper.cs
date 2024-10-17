using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Api.Mappers
{
    /// <summary>
    /// Provides extension methods for mapping CallbackRequestModel to CallbackCommand.
    /// </summary>
    public static class CallbackCommandMapper
    {
        /// <summary>
        /// Maps a <see cref="CallbackRequestModel"/> to a <see cref="CallbackCommand"/>.
        /// </summary>
        /// <param name="model">The CallbackRequestModel to map.</param>
        /// <returns>A CallbackCommand with the mapped values.</returns>
        public static CallbackCommand MapToCommand(this CallbackRequestModel model)
        {
            return new CallbackCommand
            {
                Code = model.Code,
                State = model.State,
            };
        }
    }
}

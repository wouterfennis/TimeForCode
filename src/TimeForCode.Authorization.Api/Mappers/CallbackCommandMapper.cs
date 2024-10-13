using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Api.Mappers
{
    public static class CallbackCommandMapper
    {
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

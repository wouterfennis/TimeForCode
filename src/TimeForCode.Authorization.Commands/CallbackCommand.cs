using MediatR;

namespace TimeForCode.Authorization.Commands
{
    public class CallbackCommand : IRequest<Uri>
    {
        public required string Code { get; init; }

        public required string State { get; init; }
    }
}
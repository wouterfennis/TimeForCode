using MediatR;

namespace TimeForCode.Authorization.Commands
{
    public class LogoutCommand : IRequest
    {
        public required Values.RefreshToken? RefreshToken { get; init; }
    }
}
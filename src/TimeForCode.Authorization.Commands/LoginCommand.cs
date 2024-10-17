using MediatR;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Commands
{
    public class LoginCommand : IRequest<Uri>
    {
        public required IdentityProvider IdentityProvider { get; init; }
    }
}
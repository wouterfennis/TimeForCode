using MediatR;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Commands
{
    public class LoginCommand : IRequest<Uri>
    {
        public IdentityProvider IdentityProvider { get; set; }
    }
}

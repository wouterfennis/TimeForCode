using MediatR;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Commands
{
    public class RefreshCommand : IRequest<Result<TokenResult>>
    {
        public required RefreshToken RefreshToken { get; init; }
    }
}
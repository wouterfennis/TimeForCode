using MediatR;

namespace TimeForCode.Authorization.Commands
{
    public class ExchangeCodeCommand : IRequest<Result<TokenResult>>
    {
        public required string Code { get; init; }

        public required string CodeVerifier { get; init; }
    }
}
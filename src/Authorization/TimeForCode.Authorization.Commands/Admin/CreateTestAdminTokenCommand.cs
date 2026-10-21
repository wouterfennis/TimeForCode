using MediatR;

namespace TimeForCode.Authorization.Commands.Admin
{
    /// <summary>
    /// Issues an admin token for live tests without a passkey ceremony. Only succeeds while the test-only admin access is enabled.
    /// </summary>
    public class CreateTestAdminTokenCommand : IRequest<Result<TokenResult>>
    {
        public required string Secret { get; init; }
    }
}
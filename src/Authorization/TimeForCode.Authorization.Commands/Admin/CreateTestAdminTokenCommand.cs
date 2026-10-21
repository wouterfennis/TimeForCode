using MediatR;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Commands.Admin
{
    /// <summary>
    /// Issues a short-lived admin access token for live tests without a passkey ceremony. Only succeeds while the test-only admin access is enabled.
    /// </summary>
    public class CreateTestAdminTokenCommand : IRequest<Result<AccessToken>>
    {
        public required string Secret { get; init; }
    }
}
using MediatR;

namespace TimeForCode.Authorization.Commands
{
    /// <summary>
    /// Removes the account, its refresh tokens and the identity on the contributions of the user.
    /// </summary>
    public class DeleteAccountCommand : IRequest<Result<Unit>>
    {
        public required string UserId { get; init; }
    }
}
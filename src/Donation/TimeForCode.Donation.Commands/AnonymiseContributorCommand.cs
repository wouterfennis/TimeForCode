using MediatR;

namespace TimeForCode.Donation.Commands
{
    /// <summary>
    /// Replaces the identity of a contributor, and the identity snapshot on their contributions, with a generic deleted user.
    /// </summary>
    public class AnonymiseContributorCommand : IRequest<Result>
    {
        public required string UserId { get; init; }
    }
}
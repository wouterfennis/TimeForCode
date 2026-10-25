using MediatR;

namespace TimeForCode.Donation.Commands
{
    /// <summary>
    /// Returns the maintainer profile of the caller.
    /// </summary>
    public class GetMaintainerProfileQuery : IRequest<Result<MaintainerProfileResult>>
    {
        public required string UserId { get; init; }
    }
}
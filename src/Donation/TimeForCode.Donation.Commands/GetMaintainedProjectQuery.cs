using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetMaintainedProjectQuery : IRequest<Result<GetMaintainedProjectResult>>
    {
        public required string ProjectId { get; init; }
        public required string UserId { get; init; }
    }
}
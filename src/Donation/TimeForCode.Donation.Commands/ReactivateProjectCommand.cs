using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class ReactivateProjectCommand : IRequest<Result<ProjectLifecycleResult>>
    {
        public required string ProjectId { get; init; }
        public required string UserId { get; init; }
    }
}
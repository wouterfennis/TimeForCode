using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class ApproveProjectCommand : IRequest<Result<ProjectLifecycleResult>>
    {
        public required string ProjectId { get; init; }
    }
}
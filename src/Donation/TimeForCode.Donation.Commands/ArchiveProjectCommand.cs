using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class ArchiveProjectCommand : IRequest<Result<ProjectLifecycleResult>>
    {
        public required string ProjectId { get; init; }
        public required string UserId { get; init; }
    }
}
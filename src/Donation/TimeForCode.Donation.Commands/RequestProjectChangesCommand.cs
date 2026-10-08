using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class RequestProjectChangesCommand : IRequest<Result<ProjectLifecycleResult>>
    {
        public required string ProjectId { get; init; }
        public required string Reason { get; init; }
    }
}
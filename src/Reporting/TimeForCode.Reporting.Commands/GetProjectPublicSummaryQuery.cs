using MediatR;

namespace TimeForCode.Reporting.Commands
{
    public class GetProjectPublicSummaryQuery : IRequest<Result<ProjectPublicSummaryResult>>
    {
        public required string ProjectId { get; init; }
    }
}
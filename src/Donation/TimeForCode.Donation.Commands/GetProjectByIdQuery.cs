using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetProjectByIdQuery : IRequest<Result<GetProjectByIdResult>>
    {
        public required string ProjectId { get; init; }
    }
}
using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class UnpublishProjectCommand : IRequest<Result<UnpublishProjectResult>>
    {
        public required string ProjectId { get; init; }
        public required string UserId { get; init; }
    }
}

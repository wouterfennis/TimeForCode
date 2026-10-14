using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetContributionsQuery : IRequest<Result<GetContributionsResult>>
    {
        public required string ProjectId { get; init; }
    }
}
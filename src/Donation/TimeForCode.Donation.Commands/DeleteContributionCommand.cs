using MediatR;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Commands
{
    public class DeleteContributionCommand : IRequest<Result<ContributionChangedResult>>
    {
        public required string ProjectId { get; init; }
        public required string ContributionId { get; init; }
        public required Actor Actor { get; init; }
    }
}
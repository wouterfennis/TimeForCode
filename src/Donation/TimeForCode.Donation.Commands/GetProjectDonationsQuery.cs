using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetProjectDonationsQuery : IRequest<Result<GetDonationsResult>>
    {
        public required string ProjectId { get; init; }
    }
}
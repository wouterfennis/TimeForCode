using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetMyDonationsQuery : IRequest<Result<GetDonationsResult>>
    {
        public required string UserId { get; init; }
    }
}
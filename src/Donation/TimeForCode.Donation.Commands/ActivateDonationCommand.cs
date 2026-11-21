using MediatR;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Commands
{
    public class ActivateDonationCommand : IRequest<Result<DonationResult>>
    {
        public required string DonationId { get; init; }
        public required Actor Actor { get; init; }
    }
}
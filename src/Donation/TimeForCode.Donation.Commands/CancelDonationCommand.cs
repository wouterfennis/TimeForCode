using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class CancelDonationCommand : IRequest<Result<DonationResult>>
    {
        public required string DonationId { get; init; }
        public required string UserId { get; init; }
        public bool IsAdministrator { get; init; }
    }
}

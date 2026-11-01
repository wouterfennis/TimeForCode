using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationDonationsQuery : IRequest<Result<GetDonationsResult>>
    {
        public required string DonorOrganizationId { get; init; }
        public required string CallerUserId { get; init; }
        public bool IsAdministrator { get; init; }
    }
}
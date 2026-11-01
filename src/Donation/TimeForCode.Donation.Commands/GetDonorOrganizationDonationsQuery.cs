using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationDonationsQuery : IRequest<Result<GetDonorOrganizationDonationsResult>>
    {
        public required string DonorOrganizationId { get; init; }
        public required string CallerUserId { get; init; }
        public bool IsAdministrator { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
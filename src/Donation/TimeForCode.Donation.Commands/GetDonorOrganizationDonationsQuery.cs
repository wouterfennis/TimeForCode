using MediatR;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationDonationsQuery : IRequest<Result<GetDonorOrganizationDonationsResult>>
    {
        public required string DonorOrganizationId { get; init; }
        public required Actor Actor { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
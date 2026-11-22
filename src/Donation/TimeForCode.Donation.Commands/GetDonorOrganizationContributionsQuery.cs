using MediatR;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationContributionsQuery : IRequest<Result<GetDonorOrganizationContributionsResult>>
    {
        public required string DonorOrganizationId { get; init; }
        public required Actor Actor { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
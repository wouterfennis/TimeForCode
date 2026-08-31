using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationsQuery : IRequest<Result<GetDonorOrganizationsResult>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
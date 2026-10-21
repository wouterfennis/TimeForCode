using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationContributorsQuery : IRequest<Result<GetDonorOrganizationContributorsResult>>
    {
        public required string DonorOrganizationId { get; init; }
    }
}
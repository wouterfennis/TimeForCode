using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationContributionsQuery : IRequest<Result<GetContributionsResult>>
    {
        public required string DonorOrganizationId { get; init; }
        public required string CallerUserId { get; init; }
        public bool IsAdministrator { get; init; }
    }
}
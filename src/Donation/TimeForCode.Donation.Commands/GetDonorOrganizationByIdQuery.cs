using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationByIdQuery : IRequest<Result<GetDonorOrganizationByIdResult>>
    {
        public required string Id { get; init; }
    }
}
using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class DeleteDonorOrganizationCommand : IRequest<Result<DeleteDonorOrganizationResult>>
    {
        public required string Id { get; init; }
    }
}
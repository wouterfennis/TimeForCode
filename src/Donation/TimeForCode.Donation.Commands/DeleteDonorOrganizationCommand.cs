using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class DeleteDonorOrganizationCommand : IRequest<Result<IDeleteDonorOrganizationResult>>
    {
        public required string Id { get; init; }
    }
}
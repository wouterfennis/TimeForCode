using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class DeleteDonorOrganizationCommand : IRequest<Result>
    {
        public required string Id { get; init; }
    }
}
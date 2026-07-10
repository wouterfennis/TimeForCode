using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class UpdateDonorOrganizationCommand : IRequest<Result<UpdateDonorOrganizationResult>>
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? ContactEmail { get; init; }
        public string? Website { get; init; }
    }
}
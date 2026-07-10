using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class CreateDonorOrganizationCommand : IRequest<Result<CreateDonorOrganizationResult>>
    {
        public required string Name { get; init; }
        public string? ContactEmail { get; init; }
        public string? Website { get; init; }
    }
}
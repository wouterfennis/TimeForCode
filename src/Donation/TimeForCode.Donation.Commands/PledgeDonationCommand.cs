using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class PledgeDonationCommand : IRequest<Result<DonationResult>>
    {
        public const int MaxHours = 10_000;

        public required string ProjectId { get; init; }
        public required string UserId { get; init; }
        public required int Hours { get; init; }

        /// <summary>
        /// Pledge as an individual; exclusive with <see cref="DonorOrganizationId"/>.
        /// </summary>
        public bool AsIndividual { get; init; }

        /// <summary>
        /// Pledge for a donor organization; exclusive with <see cref="AsIndividual"/>.
        /// </summary>
        public string? DonorOrganizationId { get; init; }
    }
}

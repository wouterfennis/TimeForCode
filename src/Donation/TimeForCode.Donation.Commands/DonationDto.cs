using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Commands
{
    public class DonationDto
    {
        public required string Id { get; init; }
        public required string ProjectId { get; init; }
        public string? DonorOrganizationId { get; init; }
        public required DonationStatus Status { get; init; }
        public required int HoursCommitted { get; init; }
        public required int HoursLogged { get; init; }
        public required int ProgressPercentage { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
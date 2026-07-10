namespace TimeForCode.Donation.Commands
{
    public class DonorOrganizationDto
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? ContactEmail { get; init; }
        public string? Website { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
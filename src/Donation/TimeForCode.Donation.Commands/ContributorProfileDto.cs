namespace TimeForCode.Donation.Commands
{
    public class ContributorProfileDto
    {
        public required string Id { get; init; }
        public required string GithubLogin { get; init; }
        public required string AvatarUrl { get; init; }
        public string? Role { get; init; }
        public required IReadOnlyList<string> Skills { get; init; }
        public int? HoursAvailable { get; init; }
        public string? DonorOrganizationId { get; init; }
    }
}
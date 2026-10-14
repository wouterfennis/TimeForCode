namespace TimeForCode.Donation.Commands
{
    public class ContributionDto
    {
        public required string Id { get; init; }
        public required string ProjectId { get; init; }
        public required string ContributorId { get; init; }
        public required string ContributorGitHubLogin { get; init; }
        public required string ContributorAvatarUrl { get; init; }
        public required int Hours { get; init; }
        public string? GithubReference { get; init; }
        public required DateTimeOffset LoggedAt { get; init; }
    }
}
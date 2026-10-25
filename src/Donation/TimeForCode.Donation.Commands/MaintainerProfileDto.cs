namespace TimeForCode.Donation.Commands
{
    public class MaintainerProfileDto
    {
        public required string Id { get; init; }
        public required string GithubLogin { get; init; }
        public required string AvatarUrl { get; init; }
    }
}
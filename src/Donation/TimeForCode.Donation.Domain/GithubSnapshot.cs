namespace TimeForCode.Donation.Domain
{
    public record GithubSnapshot
    {
        public required string Name { get; init; }
        public required string FullName { get; init; }
        public string? Description { get; init; }
        public required string HtmlUrl { get; init; }
        public string? Language { get; init; }
        public required IReadOnlyList<string> Topics { get; init; }
        public int StargazersCount { get; init; }
        public int ForksCount { get; init; }
        public int OpenIssuesCount { get; init; }
        public string? Homepage { get; init; }
        public required string DefaultBranch { get; init; }
        public string? License { get; init; }
        public required string OwnerLogin { get; init; }
        public required string OwnerAvatarUrl { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset UpdatedAt { get; init; }
        public required DateTimeOffset PushedAt { get; init; }
        public bool IsPrivate { get; init; }
        public bool IsArchived { get; init; }
    }
}
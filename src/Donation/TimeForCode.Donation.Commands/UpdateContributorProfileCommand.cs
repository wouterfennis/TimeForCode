using MediatR;

namespace TimeForCode.Donation.Commands
{
    /// <summary>
    /// Replaces a contributor profile: the caller's own (registering the contributor when needed) when <see cref="ContributorId"/> is empty,
    /// otherwise the profile with that id (own profile or administrator only).
    /// </summary>
    public class UpdateContributorProfileCommand : IRequest<Result<ContributorProfileResult>>
    {
        public required string CallerUserId { get; init; }
        public required string GithubLogin { get; init; }
        public required string AvatarUrl { get; init; }
        public bool IsAdministrator { get; init; }
        public string? ContributorId { get; init; }
        public string? Role { get; init; }
        public required IReadOnlyList<string> Skills { get; init; }
        public int? HoursAvailable { get; init; }
        public string? DonorOrganizationId { get; init; }
    }
}
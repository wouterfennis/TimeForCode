using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;

namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// A single time-log entry of a contributor against a project. Holds a reference to the
    /// <see cref="Contributor"/> document plus a snapshot of its display fields (extended reference pattern).
    /// </summary>
    public class Contribution : DocumentEntity
    {
        public required ObjectId ProjectId { get; init; }
        public required ObjectId ContributorId { get; init; }
        public required string ContributorGitHubLogin { get; init; }
        public required string ContributorAvatarUrl { get; init; }
        public required int Hours { get; init; }
        public Uri? GithubReference { get; init; }
        public required DateTimeOffset LoggedAt { get; init; }

        public static Contribution Create(
            ObjectId projectId,
            Contributor contributor,
            int hours,
            Uri? githubReference,
            DateTimeOffset loggedAt)
        {
            return new Contribution
            {
                Id = ObjectId.GenerateNewId(),
                ProjectId = projectId,
                ContributorId = contributor.Id,
                ContributorGitHubLogin = contributor.GithubLogin,
                ContributorAvatarUrl = contributor.AvatarUrl,
                Hours = hours,
                GithubReference = githubReference,
                LoggedAt = loggedAt
            };
        }
    }
}
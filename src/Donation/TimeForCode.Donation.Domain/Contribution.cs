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

        /// <summary>The values before each correction, oldest first.</summary>
        public IReadOnlyList<ContributionCorrection> History { get; init; } = [];

        public DateTimeOffset? DeletedAt { get; init; }
        public string? DeletedByUserId { get; init; }

        public bool IsDeleted => DeletedAt != null;

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

        /// <summary>Returns a copy with the new hours and reference; the replaced values are appended to the history.</summary>
        public Contribution Correct(string correctedByUserId, int hours, Uri? githubReference, DateTimeOffset correctedAt)
        {
            var entry = new ContributionCorrection
            {
                Hours = Hours,
                GithubReference = GithubReference,
                CorrectedByUserId = correctedByUserId,
                CorrectedAt = correctedAt
            };

            return Copy(hours, githubReference, [.. History, entry], DeletedAt, DeletedByUserId);
        }

        /// <summary>Returns a copy marked as deleted; the values stay stored.</summary>
        public Contribution MarkDeleted(string deletedByUserId, DateTimeOffset deletedAt)
        {
            return Copy(Hours, GithubReference, History, deletedAt, deletedByUserId);
        }

        /// <summary>Only the contributor who logged the contribution and administrators may change it.</summary>
        public bool CanBeChangedBy(string userId, Contributor? contributor, bool isAdministrator)
        {
            return isAdministrator || (contributor != null && contributor.Id == ContributorId && contributor.PlatformUserId == userId);
        }

        private Contribution Copy(int hours, Uri? githubReference, IReadOnlyList<ContributionCorrection> history, DateTimeOffset? deletedAt, string? deletedByUserId)
        {
            return new Contribution
            {
                Id = Id,
                ProjectId = ProjectId,
                ContributorId = ContributorId,
                ContributorGitHubLogin = ContributorGitHubLogin,
                ContributorAvatarUrl = ContributorAvatarUrl,
                Hours = hours,
                GithubReference = githubReference,
                LoggedAt = LoggedAt,
                History = history,
                DeletedAt = deletedAt,
                DeletedByUserId = deletedByUserId
            };
        }
    }
}
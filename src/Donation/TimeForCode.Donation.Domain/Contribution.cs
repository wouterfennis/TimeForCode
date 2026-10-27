using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;
using TimeForCode.Donation.Values;

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

        /// <summary>
        /// Counts the saves of the contribution; the repository only saves a change made on the version it loaded (optimistic concurrency).
        /// </summary>
        public int Version { get; init; }

        public bool IsDeleted => DeletedAt != null;

        public static Contribution Create(
            ObjectId projectId,
            Contributor contributor,
            int hours,
            Uri? githubReference,
            DateTimeOffset loggedAt)
        {
            EnsureValid(hours, githubReference);

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

        /// <summary>
        /// An HTTPS link to a page on github.com without credentials, port or query, such as a pull request or a commit.
        /// </summary>
        public static bool IsValidGithubReference(Uri? reference)
        {
            return reference != null
                && reference.IsAbsoluteUri
                && reference.OriginalString.Length <= ContributionLimits.MaxReferenceLength
                && string.Equals(reference.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && string.Equals(reference.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                && reference.IsDefaultPort
                && string.IsNullOrEmpty(reference.UserInfo)
                && string.IsNullOrEmpty(reference.Query)
                && reference.AbsolutePath.Length > 1;
        }

        /// <summary>
        /// Replaces the hours and reference of the contribution; the replaced values are appended to the history.
        /// Refused when the contribution is deleted or the user is neither the contributor nor an administrator.
        /// </summary>
        /// <param name="contributor">The contributor of the contribution, or <c>null</c> when it was anonymised.</param>
        public ContributionChange Correct(string userId, bool isAdministrator, Contributor? contributor, int hours, Uri? githubReference, DateTimeOffset correctedAt)
        {
            var refused = Refuse(userId, isAdministrator, contributor);
            if (refused != null)
            {
                return refused;
            }

            EnsureValid(hours, githubReference);

            var entry = new ContributionCorrection
            {
                Hours = Hours,
                GithubReference = GithubReference,
                CorrectedByUserId = userId,
                CorrectedAt = correctedAt
            };

            return ContributionChange.Success(Copy(hours, githubReference, [.. History, entry], DeletedAt, DeletedByUserId), hours - Hours);
        }

        /// <summary>
        /// Marks the contribution as deleted; the values stay stored. Refused when it is already deleted or the user is neither the contributor nor an administrator.
        /// </summary>
        /// <param name="contributor">The contributor of the contribution, or <c>null</c> when it was anonymised.</param>
        public ContributionChange MarkDeleted(string userId, bool isAdministrator, Contributor? contributor, DateTimeOffset deletedAt)
        {
            var refused = Refuse(userId, isAdministrator, contributor);
            if (refused != null)
            {
                return refused;
            }

            return ContributionChange.Success(Copy(Hours, GithubReference, History, deletedAt, userId), -Hours);
        }

        /// <summary>Only the contributor who logged the contribution and administrators may change it.</summary>
        private bool CanBeChangedBy(string userId, Contributor? contributor, bool isAdministrator)
        {
            return isAdministrator || (contributor != null && contributor.Id == ContributorId && contributor.PlatformUserId == userId);
        }

        private ContributionChange? Refuse(string userId, bool isAdministrator, Contributor? contributor)
        {
            if (IsDeleted)
            {
                return ContributionChange.Refused(ContributionChangeFailure.Deleted);
            }

            return CanBeChangedBy(userId, contributor, isAdministrator)
                ? null
                : ContributionChange.Refused(ContributionChangeFailure.NotAllowed);
        }

        private static void EnsureValid(int hours, Uri? githubReference)
        {
            if (hours is < 1 or > ContributionLimits.MaxHours)
            {
                throw new ArgumentOutOfRangeException(nameof(hours), $"Hours must be between 1 and {ContributionLimits.MaxHours}.");
            }

            if (githubReference != null && !IsValidGithubReference(githubReference))
            {
                throw new ArgumentException("The GitHub reference must be an HTTPS link to a page on github.com.", nameof(githubReference));
            }
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
                DeletedByUserId = deletedByUserId,
                Version = Version
            };
        }
    }
}
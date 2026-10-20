using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// A platform user who logs time on projects; persisted as its own document and upserted by <see cref="PlatformUserId"/>.
    /// </summary>
    public class Contributor : DocumentEntity
    {
        public const string DeletedUserLogin = "deleted-user";
#pragma warning disable S1075 // Fixed GitHub placeholder avatar, not an environment-specific location
        public const string DeletedUserAvatarUrl = "https://github.com/ghost.png";
#pragma warning restore S1075

        public required string PlatformUserId { get; init; }
        public required string GithubLogin { get; init; }
        public required string AvatarUrl { get; init; }
        public string? Role { get; set; }
        public List<string> Skills { get; set; } = [];
        public int? HoursAvailable { get; set; }
        public ObjectId? DonorOrganizationId { get; set; }

        /// <summary>
        /// Replaces the profile; skills are trimmed and deduplicated (case-insensitive, first spelling wins) and blank values are dropped.
        /// Nothing changes when a value is out of range.
        /// </summary>
        public void UpdateProfile(string? role, IEnumerable<string>? skills, int? hoursAvailable, ObjectId? donorOrganizationId)
        {
            var normalisedRole = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
            if (normalisedRole?.Length > ContributorProfileLimits.MaxRoleLength)
            {
                throw new ArgumentException($"Role must not exceed {ContributorProfileLimits.MaxRoleLength} characters.", nameof(role));
            }

            var normalisedSkills = NormaliseSkills(skills);
            if (normalisedSkills.Count > ContributorProfileLimits.MaxSkills)
            {
                throw new ArgumentException($"A profile can hold at most {ContributorProfileLimits.MaxSkills} skills.", nameof(skills));
            }
            if (normalisedSkills.Any(s => s.Length > ContributorProfileLimits.MaxSkillLength))
            {
                throw new ArgumentException($"A skill must not exceed {ContributorProfileLimits.MaxSkillLength} characters.", nameof(skills));
            }

            if (hoursAvailable is < 0 or > ContributorProfileLimits.MaxHoursAvailable)
            {
                throw new ArgumentOutOfRangeException(nameof(hoursAvailable), hoursAvailable, $"Hours available must be between 0 and {ContributorProfileLimits.MaxHoursAvailable}.");
            }

            Role = normalisedRole;
            Skills = normalisedSkills;
            HoursAvailable = hoursAvailable;
            DonorOrganizationId = donorOrganizationId;
        }

        private static List<string> NormaliseSkills(IEnumerable<string>? skills)
        {
            return (skills ?? [])
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static Contributor Create(string platformUserId, string githubLogin, string avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(platformUserId))
            {
                throw new ArgumentException("Platform user id cannot be null or empty.", nameof(platformUserId));
            }
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                throw new ArgumentException("Avatar URL cannot be null or empty.", nameof(avatarUrl));
            }
            if (string.IsNullOrWhiteSpace(githubLogin))
            {
                throw new ArgumentException("GitHub login cannot be null or empty.", nameof(githubLogin));
            }

            return new Contributor
            {
                Id = ObjectId.GenerateNewId(),
                PlatformUserId = platformUserId,
                GithubLogin = githubLogin,
                AvatarUrl = avatarUrl
            };
        }
    }
}
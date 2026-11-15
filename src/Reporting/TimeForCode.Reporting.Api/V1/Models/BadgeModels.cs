using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Api.V1.Models
{
    /// <summary>
    /// The badges a donor organization or a contributor holds. Public: only names and award dates, no personal data.
    /// </summary>
    public class BadgesResponse
    {
        /// <summary>The badges, earliest award first; empty when none was earned.</summary>
        public required IReadOnlyList<BadgeResponse> Badges { get; init; }
    }

    /// <summary>
    /// One badge.
    /// </summary>
    public class BadgeResponse
    {
        /// <summary>The name of the tier the badge was awarded for.</summary>
        public required string Name { get; init; }

        /// <summary>When the badge was awarded (UTC).</summary>
        public required DateTimeOffset AwardedAt { get; init; }
    }

    /// <summary>
    /// The configured badge tiers.
    /// </summary>
    public class BadgeTiersResponse
    {
        /// <summary>The tiers, grouped by recipient type and ordered by threshold.</summary>
        public required IReadOnlyList<BadgeTierResponse> Tiers { get; init; }
    }

    /// <summary>
    /// One badge tier.
    /// </summary>
    public class BadgeTierResponse
    {
        /// <summary>The tier name, unique per recipient type.</summary>
        public required string Name { get; init; }

        /// <summary>Who the tier is for: <c>Organization</c> or <c>Contributor</c>.</summary>
        public required BadgeRecipientType RecipientType { get; init; }

        /// <summary>The hours that must be logged to earn the badge, unique per recipient type.</summary>
        public required int ThresholdHours { get; init; }
    }

    /// <summary>
    /// The body for defining a badge tier.
    /// </summary>
    public class DefineBadgeTierRequest
    {
        /// <summary>The hours that must be logged to earn the badge; a positive number not used by another tier of the recipient type.</summary>
        public required int ThresholdHours { get; init; }
    }

    /// <summary>
    /// Platform-wide impact totals; no personal data.
    /// </summary>
    public class PublicStatisticsResponse
    {
        /// <summary>The hours logged on the Active and Completed donations of all donor organizations.</summary>
        public required long TotalHoursLogged { get; init; }

        /// <summary>The number of donor organizations with an Active or Completed donation.</summary>
        public required int OrganizationsSupported { get; init; }
    }
}
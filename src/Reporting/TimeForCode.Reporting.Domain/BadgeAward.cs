using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Domain
{
    /// <summary>
    /// A badge awarded to a donor organization or a contributor. At most one exists per tier and recipient.
    /// </summary>
    public record BadgeAward(string TierName, BadgeRecipientType RecipientType, string RecipientId, DateTimeOffset AwardedAt);
}
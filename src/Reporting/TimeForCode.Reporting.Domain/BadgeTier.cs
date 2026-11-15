using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Domain
{
    /// <summary>
    /// A badge milestone: reached when the recipient has logged at least <paramref name="ThresholdHours"/> hours.
    /// Names are unique per recipient type, and so are thresholds.
    /// </summary>
    public record BadgeTier(string Name, BadgeRecipientType RecipientType, int ThresholdHours);
}
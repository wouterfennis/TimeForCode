using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;

namespace TimeForCode.Reporting.Application.Handlers
{
    /// <summary>Picks the badge of the tier with the highest threshold among the badges a recipient holds.</summary>
    internal static class HighestBadge
    {
        public static BadgeImageContent From(IReadOnlyList<BadgeDto> held, IReadOnlyList<BadgeTier> tiers)
        {
            if (held.Count == 0)
            {
                return new BadgeImageContent(BadgeImageState.NoBadgeYet);
            }

            // A badge whose tier was removed since stays held; it ranks below every tier that still exists.
            // Tiers or removed badges that tie are ordered by name, so the image does not depend on the order Donation returns.
            Dictionary<string, int> thresholds = tiers
                .GroupBy(t => t.Name)
                .ToDictionary(g => g.Key, g => g.Max(t => t.ThresholdHours));

            string name = held
                .OrderByDescending(b => thresholds.GetValueOrDefault(b.Name, -1))
                .ThenBy(b => b.Name, StringComparer.Ordinal)
                .First().Name;

            return new BadgeImageContent(BadgeImageState.Tier, name);
        }
    }
}
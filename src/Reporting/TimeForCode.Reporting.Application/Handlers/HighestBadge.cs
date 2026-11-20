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
            string name = held
                .OrderByDescending(b => tiers.Where(t => t.Name == b.Name).Select(t => t.ThresholdHours).DefaultIfEmpty(-1).Max())
                .First().Name;

            return new BadgeImageContent(BadgeImageState.Tier, name);
        }
    }
}
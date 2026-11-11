using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Handlers
{
    /// <summary>
    /// Awards every badge whose threshold the recipient reached and returns the badges the recipient holds. Awarding is idempotent:
    /// the award repository stores at most one badge per tier and recipient, also when requests arrive at the same time.
    /// Because the check runs whenever badges are requested, a tier added later reaches recipients who already qualify.
    /// </summary>
    internal class BadgeAwarder
    {
        private readonly IBadgeTierRepository _tiers;
        private readonly IBadgeAwardRepository _awards;
        private readonly TimeProvider _timeProvider;

        public BadgeAwarder(IBadgeTierRepository tiers, IBadgeAwardRepository awards, TimeProvider timeProvider)
        {
            _tiers = tiers;
            _awards = awards;
            _timeProvider = timeProvider;
        }

        public async Task<IReadOnlyList<BadgeDto>> AwardAsync(BadgeRecipientType recipientType, string recipientId, int hoursLogged, CancellationToken cancellationToken)
        {
            IReadOnlyList<BadgeAward> held = await _awards.ListAsync(recipientType, recipientId, cancellationToken);
            IReadOnlyList<BadgeTier> tiers = await _tiers.ListAsync(recipientType, cancellationToken);

            // Only the tiers not yet held are written, so a read that finds nothing new (the usual case) writes nothing.
            // A request at the same time may still award the same tier; the unique index of the award repository settles that.
            List<BadgeTier> missing = tiers
                .Where(t => t.ThresholdHours <= hoursLogged && held.All(a => a.TierName != t.Name))
                .ToList();
            if (missing.Count > 0)
            {
                foreach (BadgeTier tier in missing)
                {
                    await _awards.AwardAsync(new BadgeAward(tier.Name, recipientType, recipientId, _timeProvider.GetUtcNow()), cancellationToken);
                }

                held = await _awards.ListAsync(recipientType, recipientId, cancellationToken);
            }

            return held.Select(a => new BadgeDto { Name = a.TierName, AwardedAt = a.AwardedAt }).ToList();
        }
    }
}
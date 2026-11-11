using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Specifications.Mocking
{
    /// <summary>
    /// In-memory badge tiers that keep the rules of the MongoDB repository: a name and a threshold are unique per recipient type.
    /// </summary>
    internal class InMemoryBadgeTierRepository : IBadgeTierRepository
    {
        private readonly object _gate = new();
        private readonly List<BadgeTier> _tiers = [];

        internal void Clear()
        {
            lock (_gate)
            {
                _tiers.Clear();
            }
        }

        public Task<IReadOnlyList<BadgeTier>> ListAsync(BadgeRecipientType? recipientType, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult<IReadOnlyList<BadgeTier>>(_tiers
                    .Where(t => recipientType is null || t.RecipientType == recipientType)
                    .OrderBy(t => t.RecipientType)
                    .ThenBy(t => t.ThresholdHours)
                    .ToList());
            }
        }

        public Task<bool> SaveAsync(BadgeTier tier, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                bool thresholdTaken = _tiers.Any(t => t.RecipientType == tier.RecipientType && t.ThresholdHours == tier.ThresholdHours && t.Name != tier.Name);
                if (thresholdTaken)
                {
                    return Task.FromResult(false);
                }

                _tiers.RemoveAll(t => t.RecipientType == tier.RecipientType && t.Name == tier.Name);
                _tiers.Add(tier);
                return Task.FromResult(true);
            }
        }

        public Task<bool> RemoveAsync(BadgeRecipientType recipientType, string name, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(_tiers.RemoveAll(t => t.RecipientType == recipientType && t.Name == name) > 0);
            }
        }
    }

    /// <summary>
    /// In-memory badge awards that keep the contract of the MongoDB repository: awarding is atomic and stores at most one award per tier and recipient.
    /// </summary>
    internal class InMemoryBadgeAwardRepository : IBadgeAwardRepository
    {
        private readonly object _gate = new();
        private readonly List<BadgeAward> _awards = [];

        internal void Clear()
        {
            lock (_gate)
            {
                _awards.Clear();
            }
        }

        public Task AwardAsync(BadgeAward award, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (!_awards.Any(a => a.RecipientType == award.RecipientType && a.RecipientId == award.RecipientId && a.TierName == award.TierName))
                {
                    _awards.Add(award);
                }
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BadgeAward>> ListAsync(BadgeRecipientType recipientType, string recipientId, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult<IReadOnlyList<BadgeAward>>(_awards
                    .Where(a => a.RecipientType == recipientType && a.RecipientId == recipientId)
                    .OrderBy(a => a.AwardedAt)
                    .ThenBy(a => a.TierName)
                    .ToList());
            }
        }
    }
}
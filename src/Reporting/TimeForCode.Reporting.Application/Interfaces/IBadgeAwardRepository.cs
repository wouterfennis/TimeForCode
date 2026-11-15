using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Interfaces
{
    /// <summary>
    /// The badges awarded so far.
    /// </summary>
    public interface IBadgeAwardRepository
    {
        /// <summary>
        /// Stores the award unless the recipient already holds the badge of the tier. Safe to call concurrently for the same award:
        /// exactly one call stores it.
        /// </summary>
        Task AwardAsync(BadgeAward award, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the badges of the recipient, earliest award first.
        /// </summary>
        Task<IReadOnlyList<BadgeAward>> ListAsync(BadgeRecipientType recipientType, string recipientId, CancellationToken cancellationToken);
    }
}
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Interfaces
{
    /// <summary>
    /// The configured badge tiers.
    /// </summary>
    public interface IBadgeTierRepository
    {
        /// <summary>
        /// Returns the tiers of the recipient type, or of every type when none is given, lowest threshold first.
        /// </summary>
        Task<IReadOnlyList<BadgeTier>> ListAsync(BadgeRecipientType? recipientType, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the tier, or changes the threshold of the tier with the same recipient type and name.
        /// Returns <c>false</c> without saving when another tier of the recipient type already has the threshold.
        /// </summary>
        Task<bool> SaveAsync(BadgeTier tier, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the tier; returns <c>false</c> when there was none. Badges already awarded stay.
        /// </summary>
        Task<bool> RemoveAsync(BadgeRecipientType recipientType, string name, CancellationToken cancellationToken);
    }
}
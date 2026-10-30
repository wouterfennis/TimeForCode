using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// A pledge of developer hours to one project, made by a donor organization or by an individual (never both).
    /// Moves Pending -> Active -> Completed, and can be cancelled while Pending or Active.
    /// </summary>
    public class Donation : DocumentEntity
    {
        public required ObjectId ProjectId { get; init; }

        /// <summary>
        /// The donor organization the hours are pledged for; <c>null</c> when pledged as an individual.
        /// </summary>
        public ObjectId? DonorOrganizationId { get; init; }

        /// <summary>
        /// The platform user who made the pledge; the only non-administrator allowed to change it.
        /// </summary>
        public required string PledgedByUserId { get; init; }

        public required int HoursCommitted { get; init; }
        public int HoursLogged { get; private set; }
        public DonationStatus Status { get; private set; } = DonationStatus.Pending;
        public required DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; private set; }

        /// <summary>
        /// Counts the saves of the donation; the repository only saves a change made on the version it loaded (optimistic concurrency).
        /// </summary>
        public int Version { get; init; }

        public int ProgressPercentage => HoursLogged * 100 / HoursCommitted;

        public static Donation Create(ObjectId projectId, ObjectId? donorOrganizationId, string pledgedByUserId, int hoursCommitted)
        {
            if (hoursCommitted <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hoursCommitted), "Hours must be greater than zero.");
            }

            return new Donation
            {
                Id = ObjectId.GenerateNewId(),
                ProjectId = projectId,
                DonorOrganizationId = donorOrganizationId,
                PledgedByUserId = pledgedByUserId,
                HoursCommitted = hoursCommitted,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        public bool CanBeChangedBy(string userId, bool isAdministrator)
        {
            return isAdministrator || PledgedByUserId == userId;
        }

        public DonationTransitionResult Activate()
        {
            if (Status != DonationStatus.Pending)
            {
                return DonationTransitionResult.Failure($"A donation in state {Status} cannot be activated; only Pending donations can.");
            }

            Status = DonationStatus.Active;
            UpdatedAt = DateTimeOffset.UtcNow;
            return DonationTransitionResult.Success();
        }

        public DonationTransitionResult Cancel()
        {
            if (Status is not (DonationStatus.Pending or DonationStatus.Active))
            {
                return DonationTransitionResult.Failure($"A donation in state {Status} cannot be cancelled; only Pending or Active donations can.");
            }

            Status = DonationStatus.Cancelled;
            UpdatedAt = DateTimeOffset.UtcNow;
            return DonationTransitionResult.Success();
        }

        /// <summary>
        /// Counts hours toward the given donations in the given order; what one donation cannot absorb spills over to the next.
        /// Lazy: a donation only takes its hours when the sequence reaches it, so the caller can save each one before the next is touched.
        /// </summary>
        /// <returns>The donations that took hours, with the number of hours each took.</returns>
        public static IEnumerable<CountedHours> CountHours(IEnumerable<Donation> donations, int hours)
        {
            var remaining = hours;

            foreach (var donation in donations)
            {
                if (remaining == 0)
                {
                    yield break;
                }

                var applied = donation.LogHours(remaining);
                if (applied == 0)
                {
                    continue;
                }

                remaining -= applied;
                yield return new CountedHours(donation, applied);
            }
        }

        /// <summary>
        /// Takes hours back that were counted from a contribution that was corrected or deleted. A Completed donation that falls below its commitment is Active again;
        /// a Cancelled donation stays Cancelled.
        /// </summary>
        /// <returns>The number of hours that were released; never more than are logged.</returns>
        public int ReleaseHours(int hours)
        {
            var released = Math.Min(hours, HoursLogged);
            HoursLogged -= released;
            if (Status == DonationStatus.Completed && HoursLogged < HoursCommitted)
            {
                Status = DonationStatus.Active;
            }

            UpdatedAt = DateTimeOffset.UtcNow;
            return released;
        }

        /// <summary>
        /// Counts logged hours toward the commitment of an Active donation and completes it once all pledged hours are logged.
        /// </summary>
        /// <returns>The number of hours that were counted; the rest is left for other donations.</returns>
        public int LogHours(int hours)
        {
            if (Status != DonationStatus.Active)
            {
                return 0;
            }

            var applied = Math.Min(hours, HoursCommitted - HoursLogged);
            HoursLogged += applied;
            if (HoursLogged == HoursCommitted)
            {
                Status = DonationStatus.Completed;
            }

            UpdatedAt = DateTimeOffset.UtcNow;
            return applied;
        }
    }
}
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

using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;

namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// Records that hours of one <see cref="Contribution"/> were counted toward one <see cref="Donation"/>.
    /// A contribution has zero, one or several allocations; they are written once and never changed.
    /// </summary>
    public class DonationAllocation : DocumentEntity
    {
        public required ObjectId ContributionId { get; init; }
        public required ObjectId DonationId { get; init; }
        public required int Hours { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }

        public static DonationAllocation Create(ObjectId contributionId, ObjectId donationId, int hours)
        {
            if (hours <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hours), "Hours must be greater than zero.");
            }

            return new DonationAllocation
            {
                Id = ObjectId.GenerateNewId(),
                ContributionId = contributionId,
                DonationId = donationId,
                Hours = hours,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
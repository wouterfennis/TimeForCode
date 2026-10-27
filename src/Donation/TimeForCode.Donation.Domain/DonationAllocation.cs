using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;

namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// Records that hours of one <see cref="Contribution"/> were counted toward one <see cref="Donation"/>.
    /// A contribution has zero, one or several allocations; they are written once and only shrink or disappear when hours are released.
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

        /// <summary>Returns a copy that keeps the hours that remain after part of them was released; releasing all of them removes the allocation instead.</summary>
        public DonationAllocation Reduce(int hours)
        {
            if (hours <= 0 || hours >= Hours)
            {
                throw new ArgumentOutOfRangeException(nameof(hours), "Hours must be greater than zero and less than the allocated hours.");
            }

            return new DonationAllocation
            {
                Id = Id,
                ContributionId = ContributionId,
                DonationId = DonationId,
                Hours = Hours - hours,
                CreatedAt = CreatedAt
            };
        }
    }
}
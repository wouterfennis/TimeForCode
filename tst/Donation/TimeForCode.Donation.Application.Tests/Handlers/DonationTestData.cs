using MongoDB.Bson;
using TimeForCode.Donation.Values;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    internal static class DonationTestData
    {
        internal static readonly ObjectId ProjectId = new("5f43a0e74b12c84f1b000001");
        internal static readonly ObjectId OrganizationId = new("5f43a0e74b12c84f1b0000d1");

        /// <summary>
        /// The time the test data is built at, and the time a <see cref="FixedTimeProvider"/> in handler tests returns.
        /// </summary>
        internal static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Builds a donation in the requested state through its own transitions, like production code would.
        /// </summary>
        internal static DonationEntity Build(
            DonationStatus status,
            string pledgedByUserId = "user-1",
            int committed = 20,
            int logged = 0,
            ObjectId? organizationId = null)
        {
            var donation = DonationEntity.Create(ProjectId, organizationId, pledgedByUserId, committed, Now);

            if (status != DonationStatus.Pending)
            {
                donation.Activate(Now);
            }

            if (logged > 0)
            {
                donation.LogHours(logged, Now);
            }

            if (status == DonationStatus.Completed)
            {
                donation.LogHours(committed - logged, Now);
            }

            if (status == DonationStatus.Cancelled)
            {
                donation.Cancel(Now);
            }

            return donation;
        }
    }
}
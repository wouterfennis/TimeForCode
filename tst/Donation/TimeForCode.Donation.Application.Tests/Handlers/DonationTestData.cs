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
        /// Builds a donation in the requested state through its own transitions, like production code would.
        /// </summary>
        internal static DonationEntity Build(
            DonationStatus status,
            string pledgedByUserId = "user-1",
            int committed = 20,
            int logged = 0,
            ObjectId? organizationId = null)
        {
            var donation = DonationEntity.Create(ProjectId, organizationId, pledgedByUserId, committed);

            if (status != DonationStatus.Pending)
            {
                donation.Activate();
            }

            if (logged > 0)
            {
                donation.LogHours(logged);
            }

            if (status == DonationStatus.Completed)
            {
                donation.LogHours(committed - logged);
            }

            if (status == DonationStatus.Cancelled)
            {
                donation.Cancel();
            }

            return donation;
        }
    }
}
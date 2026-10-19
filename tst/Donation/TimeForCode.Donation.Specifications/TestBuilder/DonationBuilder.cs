using MongoDB.Bson;
using TimeForCode.Donation.Values;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Specifications.TestBuilder
{
    internal static class DonationBuilder
    {
        /// <summary>
        /// Builds a donation in the requested state through its own transitions, like production code would.
        /// </summary>
        internal static DonationEntity Build(
            ObjectId projectId,
            DonationStatus status,
            string pledgedByUserId,
            int committed,
            int logged,
            ObjectId? organizationId = null)
        {
            var donation = DonationEntity.Create(projectId, organizationId, pledgedByUserId, committed);

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
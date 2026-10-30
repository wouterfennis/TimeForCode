using MongoDB.Bson;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IDonationHoursReleaser
    {
        /// <summary>
        /// Takes hours of a corrected or deleted contribution back from the donations they were counted toward, newest allocation first.
        /// A Completed donation that falls below its commitment is Active again. Nothing is counted anew.
        /// </summary>
        Task ReleaseAsync(ObjectId contributionId, int hours, CancellationToken cancellationToken);
    }
}
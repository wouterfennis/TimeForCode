using MongoDB.Bson;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IDonationHoursCounter
    {
        /// <summary>
        /// Counts hours of a contribution toward the Active donations of the project, oldest pledge first; what one donation cannot absorb spills over to the next.
        /// </summary>
        Task CountAsync(string projectId, ObjectId contributionId, int hours, CancellationToken cancellationToken);
    }
}
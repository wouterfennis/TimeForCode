namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// The total hours a contributor logged.
    /// </summary>
    public class ContributorHoursResponse
    {
        /// <summary>The total hours of all non-deleted contributions of the contributor.</summary>
        public required int TotalHours { get; init; }
    }
}
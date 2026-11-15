namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Platform-wide totals; no personal data.
    /// </summary>
    public class PlatformStatisticsResponse
    {
        /// <summary>The hours logged on all Active and Completed donations of donor organizations.</summary>
        public required long TotalHoursLogged { get; init; }

        /// <summary>The number of donor organizations with an Active or Completed donation.</summary>
        public required int OrganizationsSupported { get; init; }
    }
}
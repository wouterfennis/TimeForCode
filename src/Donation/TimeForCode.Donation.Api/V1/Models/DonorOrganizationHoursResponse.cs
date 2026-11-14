namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// The hours logged towards the donations of a donor organization.
    /// </summary>
    public class DonorOrganizationHoursResponse
    {
        /// <summary>The hours logged on the Active and Completed donations of the organization.</summary>
        public required int HoursLogged { get; init; }
    }
}
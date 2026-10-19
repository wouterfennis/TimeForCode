namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a list of donations.
    /// </summary>
    public class GetDonationsResponse
    {
        /// <summary>The donations, oldest pledge first.</summary>
        public required IReadOnlyList<DonationResponse> Donations { get; init; }
    }
}
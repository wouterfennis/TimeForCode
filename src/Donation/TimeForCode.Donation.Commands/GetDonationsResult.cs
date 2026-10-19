namespace TimeForCode.Donation.Commands
{
    public class GetDonationsResult
    {
        public required IReadOnlyList<DonationDto> Donations { get; init; }
    }
}

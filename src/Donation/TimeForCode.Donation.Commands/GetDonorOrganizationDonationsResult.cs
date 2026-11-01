namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationDonationsResult
    {
        public required IReadOnlyList<DonationDto> Donations { get; init; }
        public required int TotalCount { get; init; }
        public required int PageNumber { get; init; }
        public required int PageSize { get; init; }
    }
}
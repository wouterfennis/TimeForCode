namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationsResult
    {
        public required IReadOnlyList<DonorOrganizationDto> Organizations { get; init; }
        public required int TotalCount { get; init; }
        public required int PageNumber { get; init; }
        public required int PageSize { get; init; }
    }
}
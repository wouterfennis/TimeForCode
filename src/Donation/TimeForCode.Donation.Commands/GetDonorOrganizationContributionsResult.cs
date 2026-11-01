namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationContributionsResult
    {
        public required IReadOnlyList<ContributionDto> Contributions { get; init; }
        public required int TotalCount { get; init; }
        public required int PageNumber { get; init; }
        public required int PageSize { get; init; }
    }
}
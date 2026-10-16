namespace TimeForCode.Donation.Commands
{
    public class GetContributionsResult
    {
        public required IReadOnlyList<ContributionDto> Contributions { get; init; }
    }
}
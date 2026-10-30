namespace TimeForCode.Donation.Commands
{
    /// <summary>The contribution after it was corrected or soft deleted.</summary>
    public class ContributionChangedResult
    {
        public required ContributionDto Contribution { get; init; }
    }
}
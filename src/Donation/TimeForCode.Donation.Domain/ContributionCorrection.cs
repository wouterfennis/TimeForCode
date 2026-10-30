namespace TimeForCode.Donation.Domain
{
    /// <summary>The hours and GitHub reference a contribution had before a correction, with who corrected it and when.</summary>
    public class ContributionCorrection
    {
        public required int Hours { get; init; }
        public Uri? GithubReference { get; init; }
        public required string CorrectedByUserId { get; init; }
        public required DateTimeOffset CorrectedAt { get; init; }
    }
}
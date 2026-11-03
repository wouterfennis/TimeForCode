namespace TimeForCode.Reporting.Commands
{
    /// <summary>Aggregate totals of a project; holds no personal data.</summary>
    public class ProjectPublicSummaryResult
    {
        public required int HoursReceived { get; init; }
        public required int HoursLogged { get; init; }
        public required int ProgressPercentage { get; init; }
        public required int DonorCount { get; init; }
        public required int ContributorCount { get; init; }
    }
}
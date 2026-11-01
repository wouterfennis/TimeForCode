namespace TimeForCode.Reporting.Commands
{
    public class ContributionHistoryExportResult
    {
        public required string Content { get; init; }
        public required string ContentType { get; init; }
        public required string FileName { get; init; }
    }
}
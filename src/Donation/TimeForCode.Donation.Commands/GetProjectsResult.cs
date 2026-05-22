namespace TimeForCode.Donation.Commands
{
    public class GetProjectsResult
    {
        public required IReadOnlyList<ProjectDto> Projects { get; init; }
        public required int TotalCount { get; init; }
        public required int PageNumber { get; init; }
        public required int PageSize { get; init; }
    }
}
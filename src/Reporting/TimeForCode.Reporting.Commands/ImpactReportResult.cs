namespace TimeForCode.Reporting.Commands
{
    public class ImpactReportResult
    {
        public required int HoursPledged { get; init; }
        public required int HoursLogged { get; init; }
        public required int ProjectsSupported { get; init; }
        public required IReadOnlyList<ProjectImpactDto> Projects { get; init; }
    }

    public class ProjectImpactDto
    {
        public required string ProjectId { get; init; }
        public required int HoursPledged { get; init; }
        public required int HoursLogged { get; init; }
    }
}
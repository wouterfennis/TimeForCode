namespace TimeForCode.Reporting.Api.V1.Models
{
    /// <summary>
    /// What a donor organization pledged and delivered. Only Active and Completed donations count.
    /// </summary>
    public class ImpactReportResponse
    {
        /// <summary>Hours committed by the Active and Completed donations of the organization.</summary>
        public required int HoursPledged { get; init; }

        /// <summary>Hours logged on those donations.</summary>
        public required int HoursLogged { get; init; }

        /// <summary>Number of different projects the donations were pledged to.</summary>
        public required int ProjectsSupported { get; init; }

        /// <summary>The breakdown per project, in the order of the first donation to each.</summary>
        public required IReadOnlyList<ProjectImpactResponse> Projects { get; init; }
    }

    /// <summary>
    /// The hours a project received from the organization.
    /// </summary>
    public class ProjectImpactResponse
    {
        /// <summary>The project identifier.</summary>
        public required string ProjectId { get; init; }

        /// <summary>Hours the organization committed to the project.</summary>
        public required int HoursPledged { get; init; }

        /// <summary>Hours logged on the project's donations of the organization.</summary>
        public required int HoursLogged { get; init; }
    }
}
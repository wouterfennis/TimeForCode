using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Domain
{
    /// <summary>
    /// The hours a project received from a donor organization: pledged and logged.
    /// </summary>
    public record ProjectImpact(string ProjectId, int HoursPledged, int HoursLogged);

    /// <summary>
    /// What a donor organization pledged and delivered. Only Active and Completed donations count, like the hours the Donation context
    /// shows as donated: a Pending donation is not yet committed and a Cancelled one no longer is.
    /// </summary>
    public class ImpactReport
    {
        private ImpactReport(IReadOnlyList<ProjectImpact> projects)
        {
            Projects = projects;
            HoursPledged = projects.Sum(p => p.HoursPledged);
            HoursLogged = projects.Sum(p => p.HoursLogged);
        }

        public int HoursPledged { get; }

        public int HoursLogged { get; }

        public int ProjectsSupported => Projects.Count;

        /// <summary>The supported projects in the order of their first donation.</summary>
        public IReadOnlyList<ProjectImpact> Projects { get; }

        public static ImpactReport From(IEnumerable<OrganizationDonation> donations)
        {
            List<ProjectImpact> projects = donations
                .Where(d => d.Status is DonationStatus.Active or DonationStatus.Completed)
                .GroupBy(d => d.ProjectId)
                .Select(g => new ProjectImpact(g.Key, g.Sum(d => d.HoursCommitted), g.Sum(d => d.HoursLogged)))
                .ToList();

            return new ImpactReport(projects);
        }
    }
}
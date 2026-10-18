using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Specifications.TestBuilder
{
    internal static class ProjectStatusTestExtensions
    {
        internal static Project InStatus(this Project project, ProjectStatus status)
        {
            if (status is ProjectStatus.PendingApproval or ProjectStatus.Active or ProjectStatus.Archived)
            {
                project.SubmitForReview();
            }

            if (status is ProjectStatus.Active or ProjectStatus.Archived)
            {
                project.Approve();
            }

            if (status == ProjectStatus.Archived)
            {
                project.Archive();
            }

            return project;
        }
    }
}
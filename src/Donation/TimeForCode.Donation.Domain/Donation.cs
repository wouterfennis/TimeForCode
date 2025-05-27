namespace TimeForCode.Donation.Domain
{
    public class Donation
    {
        public DonorOrganization Donor { get; private init; }
        public Project Project { get; private init; }
        public int Hours { get; private init; }

        private Donation(DonorOrganization donor, Project project, int hours)
        {
            Donor = donor;
            Project = project;
            Hours = hours;
        }

        public static Donation Create(DonorOrganization donor, Project project, int hours)
        {
            if (donor == null)
            {
                throw new ArgumentNullException(nameof(donor), "Donor cannot be null.");
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");
            }

            if (hours <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hours), "Hours must be greater than zero.");
            }
            return new Donation(donor, project, hours);
        }
    }
}

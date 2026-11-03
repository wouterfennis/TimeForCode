using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Domain
{
    /// <summary>
    /// A donation pledged to a project, as read from the Donation context.
    /// </summary>
    public record ProjectDonation(string Id, string? DonorOrganizationId, DonationStatus Status, int HoursCommitted, int HoursLogged, DateTimeOffset CreatedAt);

    /// <summary>
    /// Hours logged on a project by a contributor, as read from the Donation context.
    /// </summary>
    public record ProjectContribution(string ContributorId, string ContributorLogin, int Hours);

    /// <summary>
    /// The hours a contributor logged on a project, added up over all their contributions.
    /// </summary>
    public record ProjectContributor(string ContributorId, string ContributorLogin, int Hours);

    /// <summary>
    /// What a project received and delivered. Only Active and Completed donations count as received, like the hours the Donation
    /// context shows as donated: a Pending donation is not yet committed and a Cancelled one no longer is.
    /// </summary>
    public class ProjectDonationReport
    {
        private ProjectDonationReport(IReadOnlyList<ProjectDonation> donations, IReadOnlyList<ProjectContributor> contributors)
        {
            Donations = donations;
            Contributors = contributors;

            List<ProjectDonation> counted = donations.Where(d => d.Status is DonationStatus.Active or DonationStatus.Completed).ToList();
            HoursReceived = counted.Sum(d => d.HoursCommitted);
            HoursLogged = counted.Sum(d => d.HoursLogged);
            ProgressPercentage = HoursReceived == 0 ? 0 : Math.Min(100, HoursLogged * 100 / HoursReceived);

            // An individual pledge carries no donor identity, so each one counts as its own donor.
            DonorCount = counted.Where(d => d.DonorOrganizationId is not null).Select(d => d.DonorOrganizationId).Distinct().Count()
                + counted.Count(d => d.DonorOrganizationId is null);

            PendingDonations = donations.Count(d => d.Status == DonationStatus.Pending);
            ActiveDonations = donations.Count(d => d.Status == DonationStatus.Active);
            CompletedDonations = donations.Count(d => d.Status == DonationStatus.Completed);
            CancelledDonations = donations.Count(d => d.Status == DonationStatus.Cancelled);
        }

        public int HoursReceived { get; }

        public int HoursLogged { get; }

        /// <summary>Hours logged against hours received, in whole percent (0 to 100).</summary>
        public int ProgressPercentage { get; }

        public int DonorCount { get; }

        public int ContributorCount => Contributors.Count;

        public int PendingDonations { get; }

        public int ActiveDonations { get; }

        public int CompletedDonations { get; }

        public int CancelledDonations { get; }

        /// <summary>Every donation of the project, whatever its status, in the order the Donation context returned them.</summary>
        public IReadOnlyList<ProjectDonation> Donations { get; }

        /// <summary>The contributors in the order of their first contribution.</summary>
        public IReadOnlyList<ProjectContributor> Contributors { get; }

        public static ProjectDonationReport From(IEnumerable<ProjectDonation> donations, IEnumerable<ProjectContribution> contributions)
        {
            List<ProjectContributor> contributors = contributions
                .GroupBy(c => c.ContributorId)
                .Select(g => new ProjectContributor(g.Key, g.First().ContributorLogin, g.Sum(c => c.Hours)))
                .ToList();

            return new ProjectDonationReport(donations.ToList(), contributors);
        }
    }
}
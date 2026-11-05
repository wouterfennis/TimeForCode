using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Api.V1.Models
{
    /// <summary>
    /// The detailed donation report of a project: what it received, who contributed and how far it has progressed.
    /// </summary>
    public class ProjectDonationReportResponse
    {
        /// <summary>Hours committed by the Active and Completed donations of the project.</summary>
        public required int HoursReceived { get; init; }

        /// <summary>Hours logged on those donations.</summary>
        public required int HoursLogged { get; init; }

        /// <summary>Hours logged against hours received, in whole percent (0 to 100); 0 when nothing was received.</summary>
        public required int ProgressPercentage { get; init; }

        /// <summary>Number of donors of the Active and Completed donations: an organization counts once, an individual pledge counts as one donor.</summary>
        public required int DonorCount { get; init; }

        /// <summary>Number of different contributors who logged hours on the project.</summary>
        public required int ContributorCount { get; init; }

        /// <summary>Number of Pending donations.</summary>
        public required int PendingDonations { get; init; }

        /// <summary>Number of Active donations.</summary>
        public required int ActiveDonations { get; init; }

        /// <summary>Number of Completed donations.</summary>
        public required int CompletedDonations { get; init; }

        /// <summary>Number of Cancelled donations.</summary>
        public required int CancelledDonations { get; init; }

        /// <summary>Every donation of the project, whatever its status, oldest pledge first.</summary>
        public required IReadOnlyList<ProjectDonationResponse> Donations { get; init; }

        /// <summary>The contributors with their total hours, in the order of their first contribution.</summary>
        public required IReadOnlyList<ProjectContributorResponse> Contributors { get; init; }
    }

    /// <summary>
    /// A donation pledged to the project.
    /// </summary>
    public class ProjectDonationResponse
    {
        /// <summary>The donation identifier.</summary>
        public required string Id { get; init; }

        /// <summary>The donor organization of the pledge; absent when pledged as an individual.</summary>
        public string? DonorOrganizationId { get; init; }

        /// <summary>The lifecycle state: Pending, Active, Completed or Cancelled.</summary>
        public required DonationStatus Status { get; init; }

        /// <summary>The number of hours pledged.</summary>
        public required int HoursCommitted { get; init; }

        /// <summary>The number of pledged hours that were logged on the project.</summary>
        public required int HoursLogged { get; init; }

        /// <summary>The moment the pledge was made.</summary>
        public required DateTimeOffset CreatedAt { get; init; }
    }

    /// <summary>
    /// The hours a contributor logged on the project.
    /// </summary>
    public class ProjectContributorResponse
    {
        /// <summary>The contributor identifier.</summary>
        public required string ContributorId { get; init; }

        /// <summary>The GitHub login of the contributor at the time of logging.</summary>
        public required string ContributorLogin { get; init; }

        /// <summary>The total hours the contributor logged on the project.</summary>
        public required int Hours { get; init; }
    }
}
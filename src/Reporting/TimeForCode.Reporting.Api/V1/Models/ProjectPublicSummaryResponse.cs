namespace TimeForCode.Reporting.Api.V1.Models
{
    /// <summary>
    /// The aggregate totals of a project. Holds no personal data: no names, logins or identifiers of donors or contributors.
    /// </summary>
    public class ProjectPublicSummaryResponse
    {
        /// <summary>Hours committed by the Active and Completed donations of the project.</summary>
        public required int HoursReceived { get; init; }

        /// <summary>Hours logged on those donations.</summary>
        public required int HoursLogged { get; init; }

        /// <summary>Hours logged against hours received, in whole percent (0 to 100); 0 when nothing was received.</summary>
        public required int ProgressPercentage { get; init; }

        /// <summary>Number of donors of the Active and Completed donations.</summary>
        public required int DonorCount { get; init; }

        /// <summary>Number of different contributors who logged hours on the project.</summary>
        public required int ContributorCount { get; init; }
    }
}
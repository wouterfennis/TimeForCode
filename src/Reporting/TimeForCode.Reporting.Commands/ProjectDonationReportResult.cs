using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Commands
{
    public class ProjectDonationReportResult
    {
        public required int HoursReceived { get; init; }
        public required int HoursLogged { get; init; }
        public required int ProgressPercentage { get; init; }
        public required int DonorCount { get; init; }
        public required int ContributorCount { get; init; }
        public required int PendingDonations { get; init; }
        public required int ActiveDonations { get; init; }
        public required int CompletedDonations { get; init; }
        public required int CancelledDonations { get; init; }
        public required IReadOnlyList<ProjectDonationDto> Donations { get; init; }
        public required IReadOnlyList<ProjectContributorDto> Contributors { get; init; }
    }

    public class ProjectDonationDto
    {
        public required string Id { get; init; }
        public string? DonorOrganizationId { get; init; }
        public required DonationStatus Status { get; init; }
        public required int HoursCommitted { get; init; }
        public required int HoursLogged { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
    }

    public class ProjectContributorDto
    {
        public required string ContributorId { get; init; }
        public required string ContributorLogin { get; init; }
        public required int Hours { get; init; }
    }
}
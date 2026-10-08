using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Commands
{
    public class ProjectLifecycleResult
    {
        public required string ProjectId { get; init; }
        public required ProjectStatus Status { get; init; }
        public string? ReviewerReason { get; init; }
    }
}
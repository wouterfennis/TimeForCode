namespace TimeForCode.Donation.Commands
{
    public class GetMaintainedProjectResult
    {
        public required ProjectDto Project { get; init; }
        public string? ReviewerReason { get; init; }
    }
}
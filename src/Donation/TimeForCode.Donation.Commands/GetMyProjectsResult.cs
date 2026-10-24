namespace TimeForCode.Donation.Commands
{
    public class GetMyProjectsResult
    {
        public required IReadOnlyList<MaintainedProjectDto> Projects { get; init; }
    }
}
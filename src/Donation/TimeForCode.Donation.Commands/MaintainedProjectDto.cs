namespace TimeForCode.Donation.Commands
{
    public class MaintainedProjectDto
    {
        public required ProjectDto Project { get; init; }

        /// <summary>
        /// Hours committed by Active and Completed donations to the project.
        /// </summary>
        public required int HoursDonated { get; init; }
    }
}
namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a project of the maintainer together with the hours donated to it.
    /// </summary>
    public class MaintainedProjectResponse
    {
        /// <summary>The project details.</summary>
        public required ProjectResponse Project { get; init; }

        /// <summary>The hours committed by Active and Completed donations to the project.</summary>
        public required int HoursDonated { get; init; }
    }
}
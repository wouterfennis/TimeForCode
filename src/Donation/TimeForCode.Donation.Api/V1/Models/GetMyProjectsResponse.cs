namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents the projects published by the caller.
    /// </summary>
    public class GetMyProjectsResponse
    {
        /// <summary>The projects of the caller in any lifecycle state, oldest first; empty when there are none.</summary>
        public required IReadOnlyList<MaintainedProjectResponse> Projects { get; init; }
    }
}
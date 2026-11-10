namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a project of the maintainer in any lifecycle state, with the reviewer reason.
    /// </summary>
    public class GetMaintainedProjectResponse
    {
        /// <summary>The project details.</summary>
        public required ProjectResponse Project { get; init; }

        /// <summary>The reason an administrator gave when requesting changes; empty when no changes were requested.</summary>
        public string? ReviewerReason { get; init; }
    }
}
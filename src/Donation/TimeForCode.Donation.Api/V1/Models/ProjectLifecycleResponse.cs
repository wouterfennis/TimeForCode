using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents the lifecycle state of a project after a transition.
    /// </summary>
    public class ProjectLifecycleResponse
    {
        /// <summary>The project identifier.</summary>
        public required string ProjectId { get; init; }

        /// <summary>The lifecycle state the project is in now.</summary>
        public required ProjectStatus Status { get; init; }

        /// <summary>The reason an administrator gave when requesting changes; empty when no changes were requested.</summary>
        public string? ReviewerReason { get; init; }
    }
}
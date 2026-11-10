namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents the outcome of publishing a project.
    /// </summary>
    public class RegisterProjectResponse
    {
        /// <summary>The identifier of the newly created project; it starts in the draft state.</summary>
        public required string ProjectId { get; init; }
    }
}
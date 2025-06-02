namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a request to register a project.
    /// </summary>
    public class RegisterProjectRequest
    {
        /// <summary>
        /// The Github repository URL of the project.
        /// </summary>
        public required Uri GithubRepositoryUrl { get; init; }
    }
}

namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a summary of a published project for list views.
    /// </summary>
    public class ProjectSummaryResponse
    {
        /// <summary>The internal project identifier.</summary>
        public required string Id { get; init; }

        /// <summary>The repository name.</summary>
        public required string Name { get; init; }

        /// <summary>The full repository name including owner (e.g. owner/repo).</summary>
        public required string FullName { get; init; }

        /// <summary>The repository description.</summary>
        public string? Description { get; init; }

        /// <summary>The URL to the GitHub repository.</summary>
        public required string GithubUrl { get; init; }

        /// <summary>The primary programming language.</summary>
        public string? Language { get; init; }

        /// <summary>Number of stars.</summary>
        public int StargazersCount { get; init; }

        /// <summary>The owner login name.</summary>
        public required string OwnerLogin { get; init; }

        /// <summary>The owner avatar URL.</summary>
        public required string OwnerAvatarUrl { get; init; }
    }
}
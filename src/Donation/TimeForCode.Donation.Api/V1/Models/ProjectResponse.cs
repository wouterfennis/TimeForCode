using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents full project details.
    /// </summary>
    public class ProjectResponse
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

        /// <summary>Repository topics/tags.</summary>
        public required IReadOnlyList<string> Topics { get; init; }

        /// <summary>Number of stars.</summary>
        public int StargazersCount { get; init; }

        /// <summary>Number of forks.</summary>
        public int ForksCount { get; init; }

        /// <summary>Number of open issues.</summary>
        public int OpenIssuesCount { get; init; }

        /// <summary>The project homepage URL.</summary>
        public string? Homepage { get; init; }

        /// <summary>The default branch name.</summary>
        public required string DefaultBranch { get; init; }

        /// <summary>The SPDX license identifier.</summary>
        public string? License { get; init; }

        /// <summary>The owner login name.</summary>
        public required string OwnerLogin { get; init; }

        /// <summary>The owner avatar URL.</summary>
        public required string OwnerAvatarUrl { get; init; }

        /// <summary>When the repository was created on GitHub.</summary>
        public required DateTimeOffset CreatedAt { get; init; }

        /// <summary>When the repository was last updated on GitHub.</summary>
        public required DateTimeOffset UpdatedAt { get; init; }

        /// <summary>When the repository was last pushed to on GitHub.</summary>
        public required DateTimeOffset PushedAt { get; init; }

        /// <summary>The publication status.</summary>
        public required ProjectStatus Status { get; init; }
    }
}

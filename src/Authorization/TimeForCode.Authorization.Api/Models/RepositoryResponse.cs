namespace TimeForCode.Authorization.Api.Models
{
    /// <summary>
    /// Repository response model
    /// </summary>
    public class RepositoryResponse
    {
        /// <summary>
        /// Repository name
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Repository description
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Number of stars
        /// </summary>
        public int StarCount { get; init; }

        /// <summary>
        /// Primary language
        /// </summary>
        public string? Language { get; init; }

        /// <summary>
        /// Repository URL
        /// </summary>
        public required string Url { get; init; }
    }
}
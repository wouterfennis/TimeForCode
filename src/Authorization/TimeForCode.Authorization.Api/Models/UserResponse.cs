namespace TimeForCode.Authorization.Api.Models
{
    /// <summary>
    /// User response model
    /// </summary>
    public class UserResponse
    {
        /// <summary>
        /// User id
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// User login
        /// </summary>
        public required string Login { get; init; }

        /// <summary>
        /// User node id
        /// </summary>
        public required string NodeId { get; init; }

        /// <summary>
        /// User avatar url
        /// </summary>
        public required string AvatarUrl { get; init; }

        /// <summary>
        /// User name
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// User company
        /// </summary>
        public required string? Company { get; init; }

        /// <summary>
        /// User email
        /// </summary>
        public required string Email { get; init; }
    }
}
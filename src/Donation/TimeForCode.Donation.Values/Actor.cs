namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// The signed-in caller of a Donation operation: the platform user id plus whether the caller is an administrator.
    /// Keeps the "owner or administrator" pair together so the two values cannot be swapped.
    /// </summary>
    public sealed record Actor
    {
        /// <summary>Creates an actor; the user id must not be blank.</summary>
        /// <param name="userId">The platform user id of the caller.</param>
        /// <param name="isAdministrator">Whether the caller holds the administrator scope.</param>
        public Actor(string userId, bool isAdministrator)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            UserId = userId;
            IsAdministrator = isAdministrator;
        }

        /// <summary>The platform user id of the caller.</summary>
        public string UserId { get; }

        /// <summary>Whether the caller holds the administrator scope.</summary>
        public bool IsAdministrator { get; }
    }
}
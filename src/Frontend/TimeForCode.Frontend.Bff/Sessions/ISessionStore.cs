namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// Where sessions live. Shared between BFF instances (MongoDB), so a session survives a restart and
    /// the refresh lease holds across instances.
    /// </summary>
    public interface ISessionStore
    {
        Task AddSessionAsync(Session session);

        Task<Session?> FindSessionAsync(string idHash);

        Task TouchAsync(string idHash, DateTimeOffset lastSeenAt, DateTimeOffset expiresAt);

        /// <summary>
        /// Atomically takes the right to refresh and returns the id of the lease; null when another request holds an unexpired lease.
        /// </summary>
        Task<string?> TryAcquireRefreshLeaseAsync(string idHash, DateTimeOffset now, DateTimeOffset leaseUntil);

        /// <summary>
        /// Stores the rotated tokens and releases the lease, but only while the caller still holds that lease; false when it was taken over.
        /// </summary>
        Task<bool> SaveTokensAsync(string idHash, string leaseId, IssuedTokens tokens);

        Task ReleaseRefreshLeaseAsync(string idHash, string leaseId);

        Task DeleteSessionAsync(string idHash);

        Task AddLoginAttemptAsync(LoginAttempt attempt);

        /// <summary>
        /// Atomically returns and deletes the attempt, so a sign-in completes once.
        /// </summary>
        Task<LoginAttempt?> TakeLoginAttemptAsync(string idHash);
    }
}
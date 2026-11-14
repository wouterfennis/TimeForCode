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
        /// Atomically takes the right to refresh; false when another request holds an unexpired lease.
        /// </summary>
        Task<bool> TryAcquireRefreshLeaseAsync(string idHash, DateTimeOffset now, DateTimeOffset leaseUntil);

        /// <summary>
        /// Stores the rotated tokens and releases the lease.
        /// </summary>
        Task SaveTokensAsync(string idHash, IssuedTokens tokens);

        Task ReleaseRefreshLeaseAsync(string idHash);

        Task DeleteSessionAsync(string idHash);

        Task AddLoginAttemptAsync(LoginAttempt attempt);

        /// <summary>
        /// Atomically returns and deletes the attempt, so a sign-in completes once.
        /// </summary>
        Task<LoginAttempt?> TakeLoginAttemptAsync(string idHash);
    }
}
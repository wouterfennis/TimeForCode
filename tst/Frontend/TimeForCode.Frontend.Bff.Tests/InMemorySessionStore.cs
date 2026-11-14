using System.Collections.Concurrent;
using TimeForCode.Frontend.Bff.Sessions;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>The session store without a database; follows the same atomic rules as the MongoDB one.</summary>
    internal sealed class InMemorySessionStore : ISessionStore
    {
        private readonly object _gate = new();
        private readonly ConcurrentDictionary<string, Session> _sessions = new();
        private readonly ConcurrentDictionary<string, LoginAttempt> _attempts = new();

        public IReadOnlyCollection<Session> Sessions => _sessions.Values.ToList();

        public Task AddSessionAsync(Session session)
        {
            _sessions[session.IdHash] = session;
            return Task.CompletedTask;
        }

        public Task<Session?> FindSessionAsync(string idHash)
        {
            _sessions.TryGetValue(idHash, out var session);
            return Task.FromResult(session);
        }

        public Task TouchAsync(string idHash, DateTimeOffset lastSeenAt, DateTimeOffset expiresAt)
        {
            if (_sessions.TryGetValue(idHash, out var session))
            {
                session.LastSeenAt = lastSeenAt;
                session.ExpiresAt = expiresAt;
            }

            return Task.CompletedTask;
        }

        public Task<bool> TryAcquireRefreshLeaseAsync(string idHash, DateTimeOffset now, DateTimeOffset leaseUntil)
        {
            lock (_gate)
            {
                if (!_sessions.TryGetValue(idHash, out var session) || (session.RefreshLeaseUntil is { } until && until > now))
                {
                    return Task.FromResult(false);
                }

                session.RefreshLeaseUntil = leaseUntil;
                return Task.FromResult(true);
            }
        }

        public Task SaveTokensAsync(string idHash, IssuedTokens tokens)
        {
            if (_sessions.TryGetValue(idHash, out var session))
            {
                session.AccessToken = tokens.AccessToken;
                session.AccessTokenExpiresAt = tokens.AccessTokenExpiresAt;
                session.RefreshToken = tokens.RefreshToken;
                session.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;
                session.RefreshLeaseUntil = null;
            }

            return Task.CompletedTask;
        }

        public Task ReleaseRefreshLeaseAsync(string idHash)
        {
            if (_sessions.TryGetValue(idHash, out var session))
            {
                session.RefreshLeaseUntil = null;
            }

            return Task.CompletedTask;
        }

        public Task DeleteSessionAsync(string idHash)
        {
            _sessions.TryRemove(idHash, out _);
            return Task.CompletedTask;
        }

        public Task AddLoginAttemptAsync(LoginAttempt attempt)
        {
            _attempts[attempt.IdHash] = attempt;
            return Task.CompletedTask;
        }

        public Task<LoginAttempt?> TakeLoginAttemptAsync(string idHash)
        {
            _attempts.TryRemove(idHash, out var attempt);
            return Task.FromResult(attempt);
        }
    }
}
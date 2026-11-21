using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>Connection to the BFF database (configuration section <c>DbOptions</c>, as for the APIs).</summary>
    public sealed class DbOptions
    {
        public const string SectionName = "DbOptions";

        public string ConnectionString { get; init; } = string.Empty;

        public string DatabaseName { get; init; } = string.Empty;
    }

    /// <summary>
    /// Sessions and pending sign-ins in MongoDB. Every state change that must not race is one atomic update:
    /// taking a login attempt, taking the refresh lease, saving rotated tokens. TTL indexes remove expired records.
    /// </summary>
    public sealed class MongoSessionStore : ISessionStore
    {
        internal const string SessionsCollection = "BffSessions";
        internal const string LoginAttemptsCollection = "BffLoginAttempts";
        internal const string SessionHashIndexName = "ux_bffsession_idhash";
        internal const string SessionExpiryIndexName = "ttl_bffsession_expires_at";
        internal const string AttemptHashIndexName = "ux_bffloginattempt_idhash";
        internal const string AttemptExpiryIndexName = "ttl_bffloginattempt_expires_at";

        private readonly Lazy<IMongoCollection<Session>> _sessions;
        private readonly Lazy<IMongoCollection<LoginAttempt>> _attempts;

        /// <summary>
        /// The client is created on first use, so starting the host (also at build time for the OpenAPI document) needs no database.
        /// </summary>
        public MongoSessionStore(IOptions<DbOptions> options)
            : this(new Lazy<IMongoDatabase>(() =>
                new MongoClient(options.Value.ConnectionString).GetDatabase(options.Value.DatabaseName)))
        {
        }

        internal MongoSessionStore(Lazy<IMongoDatabase> database)
        {
            _sessions = new Lazy<IMongoCollection<Session>>(() =>
            {
                var collection = database.Value.GetCollection<Session>(SessionsCollection);
                collection.Indexes.CreateMany(SessionIndexModels);
                return collection;
            });
            _attempts = new Lazy<IMongoCollection<LoginAttempt>>(() =>
            {
                var collection = database.Value.GetCollection<LoginAttempt>(LoginAttemptsCollection);
                collection.Indexes.CreateMany(AttemptIndexModels);
                return collection;
            });
        }

        internal static IReadOnlyList<CreateIndexModel<Session>> SessionIndexModels { get; } =
        [
            new CreateIndexModel<Session>(
                Builders<Session>.IndexKeys.Ascending(s => s.IdHash),
                new CreateIndexOptions { Name = SessionHashIndexName, Unique = true }),
            new CreateIndexModel<Session>(
                Builders<Session>.IndexKeys.Ascending(s => s.ExpiresAt),
                new CreateIndexOptions { Name = SessionExpiryIndexName, ExpireAfter = TimeSpan.Zero })
        ];

        internal static IReadOnlyList<CreateIndexModel<LoginAttempt>> AttemptIndexModels { get; } =
        [
            new CreateIndexModel<LoginAttempt>(
                Builders<LoginAttempt>.IndexKeys.Ascending(a => a.IdHash),
                new CreateIndexOptions { Name = AttemptHashIndexName, Unique = true }),
            new CreateIndexModel<LoginAttempt>(
                Builders<LoginAttempt>.IndexKeys.Ascending(a => a.ExpiresAt),
                new CreateIndexOptions { Name = AttemptExpiryIndexName, ExpireAfter = TimeSpan.Zero })
        ];

        public Task AddSessionAsync(Session session) => _sessions.Value.InsertOneAsync(session);

        public async Task<Session?> FindSessionAsync(string idHash) =>
            await _sessions.Value.Find(s => s.IdHash == idHash).FirstOrDefaultAsync();

        public Task TouchAsync(string idHash, DateTimeOffset lastSeenAt, DateTimeOffset expiresAt) =>
            _sessions.Value.UpdateOneAsync(
                s => s.IdHash == idHash,
                Builders<Session>.Update.Set(s => s.LastSeenAt, lastSeenAt).Set(s => s.ExpiresAt, expiresAt));

        public async Task<string?> TryAcquireRefreshLeaseAsync(string idHash, DateTimeOffset now, DateTimeOffset leaseUntil)
        {
            var leaseId = Guid.NewGuid().ToString("N");
            var filter = Builders<Session>.Filter.Eq(s => s.IdHash, idHash)
                & (Builders<Session>.Filter.Eq(s => s.RefreshLeaseUntil, null) | Builders<Session>.Filter.Lte(s => s.RefreshLeaseUntil, now));

            var result = await _sessions.Value.UpdateOneAsync(
                filter,
                Builders<Session>.Update.Set(s => s.RefreshLeaseUntil, leaseUntil).Set(s => s.RefreshLeaseId, leaseId));
            return result.ModifiedCount == 1 ? leaseId : null;
        }

        public async Task<bool> SaveTokensAsync(string idHash, string leaseId, IssuedTokens tokens)
        {
            var result = await _sessions.Value.UpdateOneAsync(
                s => s.IdHash == idHash && s.RefreshLeaseId == leaseId,
                Builders<Session>.Update
                    .Set(s => s.AccessToken, tokens.AccessToken)
                    .Set(s => s.AccessTokenExpiresAt, tokens.AccessTokenExpiresAt)
                    .Set(s => s.RefreshToken, tokens.RefreshToken)
                    .Set(s => s.RefreshTokenExpiresAt, tokens.RefreshTokenExpiresAt)
                    .Set(s => s.RefreshLeaseUntil, null)
                    .Set(s => s.RefreshLeaseId, null));
            return result.ModifiedCount == 1;
        }

        public Task ReleaseRefreshLeaseAsync(string idHash, string leaseId) =>
            _sessions.Value.UpdateOneAsync(
                s => s.IdHash == idHash && s.RefreshLeaseId == leaseId,
                Builders<Session>.Update.Set(s => s.RefreshLeaseUntil, null).Set(s => s.RefreshLeaseId, null));

        public Task DeleteSessionAsync(string idHash) => _sessions.Value.DeleteOneAsync(s => s.IdHash == idHash);

        public Task AddLoginAttemptAsync(LoginAttempt attempt) => _attempts.Value.InsertOneAsync(attempt);

        public async Task<LoginAttempt?> TakeLoginAttemptAsync(string idHash) =>
            await _attempts.Value.FindOneAndDeleteAsync(a => a.IdHash == idHash);
    }
}
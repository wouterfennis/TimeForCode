using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using TimeForCode.Frontend.Bff.Sessions;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class MongoSessionStoreTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

        private Mock<IMongoCollection<Session>> _sessions = null!;
        private Mock<IMongoCollection<LoginAttempt>> _attempts = null!;
        private Mock<IMongoIndexManager<Session>> _sessionIndexes = null!;
        private Mock<IMongoIndexManager<LoginAttempt>> _attemptIndexes = null!;
        private MongoSessionStore _sut = null!;

        [TestInitialize]
        public void Initialize()
        {
            _sessions = new Mock<IMongoCollection<Session>>();
            _attempts = new Mock<IMongoCollection<LoginAttempt>>();
            _sessionIndexes = new Mock<IMongoIndexManager<Session>>();
            _attemptIndexes = new Mock<IMongoIndexManager<LoginAttempt>>();
            _sessions.SetupGet(c => c.Indexes).Returns(_sessionIndexes.Object);
            _attempts.SetupGet(c => c.Indexes).Returns(_attemptIndexes.Object);

            var database = new Mock<IMongoDatabase>();
            database.Setup(d => d.GetCollection<Session>(MongoSessionStore.SessionsCollection, null)).Returns(_sessions.Object);
            database.Setup(d => d.GetCollection<LoginAttempt>(MongoSessionStore.LoginAttemptsCollection, null)).Returns(_attempts.Object);
            _sut = new MongoSessionStore(new Lazy<IMongoDatabase>(() => database.Object));
        }

        [TestMethod]
        public async Task AddSessionAsync_InsertsTheSessionAndCreatesTheSessionIndexes()
        {
            var session = NewSession();

            await _sut.AddSessionAsync(session);

            _sessions.Verify(c => c.InsertOneAsync(session, null, It.IsAny<CancellationToken>()), Times.Once);
            _sessionIndexes.Verify(i => i.CreateMany(MongoSessionStore.SessionIndexModels, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task AddLoginAttemptAsync_InsertsTheAttemptAndCreatesTheAttemptIndexes()
        {
            var attempt = NewAttempt();

            await _sut.AddLoginAttemptAsync(attempt);

            _attempts.Verify(c => c.InsertOneAsync(attempt, null, It.IsAny<CancellationToken>()), Times.Once);
            _attemptIndexes.Verify(i => i.CreateMany(MongoSessionStore.AttemptIndexModels, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task FindSessionAsync_KnownHash_ReturnsTheSessionFilteredByHash()
        {
            var session = NewSession();
            FilterDefinition<Session>? filter = null;
            _sessions.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<Session>>(), It.IsAny<FindOptions<Session, Session>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<Session>, FindOptions<Session, Session>, CancellationToken>((f, _, _) => filter = f)
                .ReturnsAsync(Cursor(session));

            var found = await _sut.FindSessionAsync("hash");

            found.Should().BeSameAs(session);
            Render(filter!).Should().Be(new BsonDocument("IdHash", "hash").ToJson());
        }

        [TestMethod]
        public async Task TouchAsync_SetsLastSeenAndExpiryOfTheSessionWithThatHash()
        {
            var (filter, update) = CaptureUpdate(_sessions, modified: 1);

            await _sut.TouchAsync("hash", Now, Now.AddHours(8));

            Render(filter()).Should().Be(new BsonDocument("IdHash", "hash").ToJson());
            Render(update()).Should().Contain("LastSeenAt").And.Contain("ExpiresAt").And.Contain("$set");
        }

        [TestMethod]
        public async Task TryAcquireRefreshLeaseAsync_FreeLease_ReturnsTheLeaseIdAndOnlyMatchesAFreeOrExpiredLease()
        {
            var (filter, update) = CaptureUpdate(_sessions, modified: 1);

            var leaseId = await _sut.TryAcquireRefreshLeaseAsync("hash", Now, Now.AddSeconds(30));

            leaseId.Should().NotBeNullOrWhiteSpace();
            Render(filter()).Should().Contain("IdHash").And.Contain("$or").And.Contain("RefreshLeaseUntil").And.Contain("$lte");
            Render(update()).Should().Contain(leaseId!);
        }

        [TestMethod]
        public async Task TryAcquireRefreshLeaseAsync_LeaseHeldByAnotherRequest_ReturnsNull()
        {
            CaptureUpdate(_sessions, modified: 0);

            var leaseId = await _sut.TryAcquireRefreshLeaseAsync("hash", Now, Now.AddSeconds(30));

            leaseId.Should().BeNull();
        }

        [TestMethod]
        public async Task SaveTokensAsync_LeaseStillHeld_StoresTheTokensAndClearsTheLease()
        {
            var (filter, update) = CaptureUpdate(_sessions, modified: 1);

            var saved = await _sut.SaveTokensAsync("hash", "lease", new IssuedTokens("access", Now.AddMinutes(10), "refresh", Now.AddDays(7), false));

            saved.Should().BeTrue();
            Render(filter()).Should().Contain("IdHash").And.Contain("RefreshLeaseId").And.Contain("lease");
            Render(update()).Should().Contain("AccessToken").And.Contain("RefreshToken").And.Contain("RefreshLeaseId");
        }

        [TestMethod]
        public async Task SaveTokensAsync_LeaseLost_ReturnsFalse()
        {
            CaptureUpdate(_sessions, modified: 0);

            var saved = await _sut.SaveTokensAsync("hash", "lease", new IssuedTokens("access", Now.AddMinutes(10), "refresh", Now.AddDays(7), false));

            saved.Should().BeFalse();
        }

        [TestMethod]
        public async Task ReleaseRefreshLeaseAsync_ClearsOnlyTheLeaseWithThatId()
        {
            var (filter, update) = CaptureUpdate(_sessions, modified: 1);

            await _sut.ReleaseRefreshLeaseAsync("hash", "lease");

            Render(filter()).Should().Contain("RefreshLeaseId").And.Contain("lease");
            Render(update()).Should().Contain("RefreshLeaseUntil").And.Contain("RefreshLeaseId");
        }

        [TestMethod]
        public async Task DeleteSessionAsync_DeletesTheSessionWithThatHash()
        {
            FilterDefinition<Session>? filter = null;
            _sessions.Setup(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<Session>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<Session>, CancellationToken>((f, _) => filter = f)
                .ReturnsAsync(new DeleteResult.Acknowledged(1));

            await _sut.DeleteSessionAsync("hash");

            Render(filter!).Should().Be(new BsonDocument("IdHash", "hash").ToJson());
        }

        [TestMethod]
        public async Task TakeLoginAttemptAsync_KnownHash_DeletesAndReturnsTheAttemptInOneStep()
        {
            var attempt = NewAttempt();
            FilterDefinition<LoginAttempt>? filter = null;
            _attempts.Setup(c => c.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<LoginAttempt>>(), It.IsAny<FindOneAndDeleteOptions<LoginAttempt, LoginAttempt>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<LoginAttempt>, FindOneAndDeleteOptions<LoginAttempt, LoginAttempt>, CancellationToken>((f, _, _) => filter = f)
                .ReturnsAsync(attempt);

            var taken = await _sut.TakeLoginAttemptAsync("hash");

            taken.Should().BeSameAs(attempt);
            Render(filter!).Should().Be(new BsonDocument("IdHash", "hash").ToJson());
        }

        private static (Func<FilterDefinition<Session>> Filter, Func<UpdateDefinition<Session>> Update) CaptureUpdate(Mock<IMongoCollection<Session>> collection, long modified)
        {
            FilterDefinition<Session>? filter = null;
            UpdateDefinition<Session>? update = null;
            collection.Setup(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<Session>>(), It.IsAny<UpdateDefinition<Session>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<Session>, UpdateDefinition<Session>, UpdateOptions, CancellationToken>((f, u, _, _) =>
                {
                    filter = f;
                    update = u;
                })
                .ReturnsAsync(new UpdateResult.Acknowledged(modified, modified, null));
            return (() => filter!, () => update!);
        }

        private static IAsyncCursor<Session> Cursor(Session session)
        {
            var cursor = new Mock<IAsyncCursor<Session>>();
            cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
            cursor.SetupGet(c => c.Current).Returns([session]);
            return cursor.Object;
        }

        private static string Render<T>(FilterDefinition<T> filter) => filter.Render(Args<T>()).ToJson();

        private static string Render<T>(UpdateDefinition<T> update) => update.Render(Args<T>()).ToJson();

        private static RenderArgs<T> Args<T>() => new(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry);

        private static Session NewSession() => new()
        {
            IdHash = "hash",
            AccessToken = "access",
            AccessTokenExpiresAt = Now.AddMinutes(10),
            RefreshToken = "refresh",
            RefreshTokenExpiresAt = Now.AddDays(7),
            IsNewUser = false,
            CreatedAt = Now,
            LastSeenAt = Now,
            ExpiresAt = Now.AddHours(8),
            AbsoluteExpiresAt = Now.AddDays(1)
        };

        private static LoginAttempt NewAttempt() => new() { IdHash = "hash", CodeVerifier = "verifier", ReturnUrl = "/projects", ExpiresAt = Now.AddMinutes(10) };
    }
}
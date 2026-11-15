using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TimeForCode.Frontend.Bff.Sessions;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>The failure paths of the access token refresh that the in-memory store cannot force: a lost lease, a vanished session, a failing database.</summary>
    [TestClass]
    public sealed class SessionRefreshFailureTests
    {
        public TestContext TestContext { get; set; } = null!;

        private ManualTimeProvider _time = null!;
        private Mock<ISessionStore> _store = null!;
        private FakeAuthorizationApi _api = null!;
        private SessionService _sut = null!;

        [TestInitialize]
        public void Initialize()
        {
            _time = new ManualTimeProvider();
            _store = new Mock<ISessionStore>();
            _api = new FakeAuthorizationApi(_time);
            _sut = new SessionService(_store.Object, _api, Options.Create(new BrowserSessionOptions()), _time, NullLogger<SessionService>.Instance);
        }

        private Session Expired() => NewSession(_time.GetUtcNow().AddMinutes(-1));

        private Session Fresh() => NewSession(_time.GetUtcNow().AddMinutes(10));

        private Session NewSession(DateTimeOffset accessTokenExpiresAt) => new()
        {
            IdHash = "hash",
            AccessToken = "access",
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshToken = "refresh",
            RefreshTokenExpiresAt = _time.GetUtcNow().AddDays(7),
            IsNewUser = false,
            CreatedAt = _time.GetUtcNow(),
            LastSeenAt = _time.GetUtcNow(),
            ExpiresAt = _time.GetUtcNow().AddHours(8),
            AbsoluteExpiresAt = _time.GetUtcNow().AddDays(1)
        };

        private void LeaseTaken(string? leaseId = "lease") =>
            _store.Setup(s => s.TryAcquireRefreshLeaseAsync("hash", It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>())).ReturnsAsync(leaseId);

        [TestMethod]
        public async Task GetAccessTokenAsync_AnotherRequestKeepsTheLeaseTooLong_GivesUpAndReportsUnavailable()
        {
            LeaseTaken(null);
            var session = Expired();
            _store.Setup(s => s.FindSessionAsync("hash")).ReturnsAsync(session).Callback(() => _time.Advance(TimeSpan.FromSeconds(20)));

            var result = await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.Unavailable);
            _api.Refreshes.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_SessionEndsWhileWaitingForTheLease_ReportsSessionEnded()
        {
            LeaseTaken(null);
            _store.Setup(s => s.FindSessionAsync("hash")).ReturnsAsync((Session?)null);

            var result = await _sut.GetAccessTokenAsync(Expired(), TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.SessionEnded);
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_SessionGoneOnceTheLeaseIsTaken_ReportsSessionEnded()
        {
            LeaseTaken();
            _store.Setup(s => s.FindSessionAsync("hash")).ReturnsAsync((Session?)null);

            var result = await _sut.GetAccessTokenAsync(Expired(), TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.SessionEnded);
            _api.Refreshes.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_AnotherRequestRefreshedBeforeTheLeaseWasTaken_UsesItsTokenWithoutRefreshing()
        {
            LeaseTaken();
            var refreshedByAnother = Fresh();
            _store.Setup(s => s.FindSessionAsync("hash")).ReturnsAsync(refreshedByAnother);

            var result = await _sut.GetAccessTokenAsync(Expired(), TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.Available);
            result.Token.Should().Be("access");
            _api.Refreshes.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_LeaseTakenOverBeforeTheTokensAreSaved_EndsTheSession()
        {
            LeaseTaken();
            _store.Setup(s => s.FindSessionAsync("hash")).ReturnsAsync(Expired());
            _store.Setup(s => s.SaveTokensAsync("hash", "lease", It.IsAny<IssuedTokens>())).ReturnsAsync(false);

            var result = await _sut.GetAccessTokenAsync(Expired(), TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.SessionEnded);
            _store.Verify(s => s.DeleteSessionAsync("hash"), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_RotatedTokensCannotBeSavedAndTheSessionCannotBeDeleted_StillEndsTheSessionWithoutThrowing()
        {
            LeaseTaken();
            _store.Setup(s => s.FindSessionAsync("hash")).ReturnsAsync(Expired());
            _store.Setup(s => s.SaveTokensAsync("hash", "lease", It.IsAny<IssuedTokens>())).ThrowsAsync(new InvalidOperationException("database down"));
            _store.Setup(s => s.DeleteSessionAsync("hash")).ThrowsAsync(new InvalidOperationException("database down"));

            var result = await _sut.GetAccessTokenAsync(Expired(), TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.SessionEnded);
            _store.Verify(s => s.SaveTokensAsync("hash", "lease", It.IsAny<IssuedTokens>()), Times.Exactly(3));
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_LeaseCannotBeReleased_DoesNotFailTheRequest()
        {
            LeaseTaken();
            _api.RejectRefresh = true;
            _store.Setup(s => s.FindSessionAsync("hash")).ReturnsAsync(Expired());
            _store.Setup(s => s.ReleaseRefreshLeaseAsync("hash", "lease")).ThrowsAsync(new InvalidOperationException("database down"));

            var result = await _sut.GetAccessTokenAsync(Expired(), TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.SessionEnded);
        }
    }
}
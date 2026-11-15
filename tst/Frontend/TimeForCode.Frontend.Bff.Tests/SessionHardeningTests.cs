using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TimeForCode.Frontend.Bff.Sessions;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>The failure paths of the session: slow or failing Authorization API, lost leases, hostile return URLs.</summary>
    [TestClass]
    public sealed class SessionHardeningTests
    {
        public TestContext TestContext { get; set; } = null!;

        private ManualTimeProvider _time = null!;
        private InMemorySessionStore _store = null!;
        private FakeAuthorizationApi _api = null!;
        private SessionService _sut = null!;

        [TestInitialize]
        public void Initialize()
        {
            _time = new ManualTimeProvider();
            _store = new InMemorySessionStore();
            _api = new FakeAuthorizationApi(_time);
            _sut = new SessionService(_store, _api, Options.Create(new BrowserSessionOptions()), _time, NullLogger<SessionService>.Instance);
        }

        private async Task<string> SignInAsync(string? previousSessionId = null)
        {
            var start = await _sut.StartLoginAsync("/", TestContext.CancellationToken);
            var signedIn = await _sut.CompleteLoginAsync(start.LoginId, "one-time-code", previousSessionId, TestContext.CancellationToken);
            return signedIn!.SessionId;
        }

        private async Task<Session> ExpiredSessionAsync()
        {
            var sessionId = await SignInAsync();
            _time.Advance(TimeSpan.FromMinutes(16));
            return (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken))!;
        }

        [TestMethod]
        [DataRow("/\t/evil.test")]
        [DataRow("/\n/evil.test")]
        [DataRow("/\r/evil.test")]
        [DataRow("/a\\b")]
        [DataRow("/a\u007Fb")]
        public async Task CompleteLoginAsync_ReturnUrlWithControlCharactersOrBackslash_ReturnsToTheRoot(string returnUrl)
        {
            var start = await _sut.StartLoginAsync(returnUrl, TestContext.CancellationToken);

            var signedIn = await _sut.CompleteLoginAsync(start.LoginId, "one-time-code", null, TestContext.CancellationToken);

            signedIn!.ReturnUrl.Should().Be("/");
        }

        [TestMethod]
        public async Task CompleteLoginAsync_WithAPreviousSession_DiscardsIt()
        {
            var previous = await SignInAsync();

            var next = await SignInAsync(previous);

            next.Should().NotBe(previous);
            _store.Sessions.Should().ContainSingle();
            (await _sut.FindSessionAsync(previous, TestContext.CancellationToken)).Should().BeNull();
        }

        [TestMethod]
        public async Task CompleteLoginAsync_FailedSignIn_KeepsThePreviousSession()
        {
            var previous = await SignInAsync();
            var start = await _sut.StartLoginAsync("/", TestContext.CancellationToken);

            var signedIn = await _sut.CompleteLoginAsync(start.LoginId, "forged", previous, TestContext.CancellationToken);

            signedIn.Should().BeNull();
            (await _sut.FindSessionAsync(previous, TestContext.CancellationToken)).Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_RefreshTokenRejected_ReportsTheSessionEnded()
        {
            var session = await ExpiredSessionAsync();
            _api.RejectRefresh = true;

            var result = await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.SessionEnded);
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_AuthorizationApiTimesOut_KeepsTheSessionAndReleasesTheLease()
        {
            var session = await ExpiredSessionAsync();
            _api.RefreshFailure = new TaskCanceledException("timeout");

            var result = await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.Unavailable);
            var stored = _store.Sessions.Should().ContainSingle().Subject;
            stored.RefreshLeaseUntil.Should().BeNull("the next request must be able to try again at once");
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_AuthorizationApiSendsGarbage_KeepsTheSessionAndReleasesTheLease()
        {
            var session = await ExpiredSessionAsync();
            _api.RefreshFailure = new System.Text.Json.JsonException("not json");

            var result = await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.Unavailable);
            _store.Sessions.Single().RefreshLeaseUntil.Should().BeNull();
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_AfterAnOutage_RefreshesWithTheSameRefreshToken()
        {
            var session = await ExpiredSessionAsync();
            _api.RefreshFailure = new HttpRequestException("down");
            await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken);
            _api.RefreshFailure = null;

            var result = await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.Available);
            _api.Refreshes.Should().Equal("refresh-1", "refresh-1");
        }

        [TestMethod]
        public async Task SaveTokensAsync_LeaseTakenOverByAnotherRequest_DoesNotOverwriteTheNewerTokens()
        {
            var sessionId = await SignInAsync();
            var idHash = SessionIds.Hash(sessionId);
            var slow = await _store.TryAcquireRefreshLeaseAsync(idHash, _time.GetUtcNow(), _time.GetUtcNow().AddSeconds(30));
            _time.Advance(TimeSpan.FromSeconds(31));
            var fast = await _store.TryAcquireRefreshLeaseAsync(idHash, _time.GetUtcNow(), _time.GetUtcNow().AddSeconds(30));
            await _store.SaveTokensAsync(idHash, fast!, _api.Issue(isNewUser: false));

            var savedBySlow = await _store.SaveTokensAsync(idHash, slow!, _api.Issue(isNewUser: false));

            savedBySlow.Should().BeFalse();
            _store.Sessions.Single().RefreshToken.Should().Be("refresh-2");
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_SavingTheRotatedTokensFailsOnce_RetriesSoTheRotationIsNotLost()
        {
            var session = await ExpiredSessionAsync();
            _store.SaveFailures = 1;

            var result = await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.Available);
            _store.Sessions.Single().RefreshToken.Should().Be("refresh-2");
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_SavingTheRotatedTokensKeepsFailing_EndsTheSessionInsteadOfReusingTheOldRefreshToken()
        {
            var session = await ExpiredSessionAsync();
            _store.SaveFailures = 5;

            var result = await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken);

            result.Outcome.Should().Be(AccessTokenOutcome.SessionEnded);
            _store.Sessions.Should().BeEmpty("a later refresh with the old, already rotated token would revoke all tokens of the user");
        }

        [TestMethod]
        [DataRow(typeof(HttpRequestException))]
        [DataRow(typeof(TaskCanceledException))]
        [DataRow(typeof(InvalidOperationException))]
        public async Task SignOutAsync_RevokingFails_StillEndsTheSession(Type failure)
        {
            var sessionId = await SignInAsync();
            var session = (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken))!;
            _api.LogoutFailure = (Exception)Activator.CreateInstance(failure, "failed")!;

            await _sut.SignOutAsync(session, TestContext.CancellationToken);

            _store.Sessions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task TryReadProfile_MalformedAccessToken_ReturnsNull()
        {
            var sessionId = await SignInAsync();
            var session = (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken))!;
            session.AccessToken = "garbage";

            SessionService.TryReadProfile(session).Should().BeNull();
        }
    }
}
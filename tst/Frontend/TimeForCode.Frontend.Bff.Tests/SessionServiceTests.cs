using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using TimeForCode.Frontend.Bff.Sessions;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class SessionServiceTests
    {
        public TestContext TestContext { get; set; } = null!;

        private ManualTimeProvider _time = null!;
        private InMemorySessionStore _store = null!;
        private FakeAuthorizationApi _api = null!;
        private BrowserSessionOptions _options = null!;
        private SessionService _sut = null!;

        [TestInitialize]
        public void Initialize()
        {
            _time = new ManualTimeProvider();
            _store = new InMemorySessionStore();
            _api = new FakeAuthorizationApi(_time);
            _options = new BrowserSessionOptions();
            _sut = new SessionService(_store, _api, Options.Create(_options), _time, NullLogger<SessionService>.Instance);
        }

        private async Task<string?> GetTokenAsync(Session session) =>
            (await _sut.GetAccessTokenAsync(session, TestContext.CancellationToken)).Token;

        private async Task<string> SignInAsync()
        {
            var start = await _sut.StartLoginAsync("/projects", TestContext.CancellationToken);
            var signedIn = await _sut.CompleteLoginAsync(start.LoginId, "one-time-code", null, TestContext.CancellationToken);
            return signedIn!.SessionId;
        }

        [TestMethod]
        public async Task StartLoginAsync_Always_ChallengeIsTheS256OfAVerifierThatNeverLeavesTheServer()
        {
            var start = await _sut.StartLoginAsync("/projects", TestContext.CancellationToken);

            await _sut.CompleteLoginAsync(start.LoginId, "one-time-code", null, TestContext.CancellationToken);

            var verifier = _api.Exchanges.Single().Verifier;
            Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).Should().Be(start.CodeChallenge);
            start.CodeChallenge.Should().NotContain(verifier);
        }

        [TestMethod]
        [DataRow("/projects/42?tab=donations", "/projects/42?tab=donations")]
        [DataRow(null, "/")]
        [DataRow("", "/")]
        [DataRow("https://evil.test/", "/")]
        [DataRow("//evil.test/", "/")]
        [DataRow("/\\evil.test", "/")]
        [DataRow("javascript:alert(1)", "/")]
        public async Task CompleteLoginAsync_ReturnUrl_OnlyALocalPathSurvives(string? returnUrl, string expected)
        {
            var start = await _sut.StartLoginAsync(returnUrl, TestContext.CancellationToken);

            var signedIn = await _sut.CompleteLoginAsync(start.LoginId, "one-time-code", null, TestContext.CancellationToken);

            signedIn!.ReturnUrl.Should().Be(expected);
        }

        [TestMethod]
        public async Task CompleteLoginAsync_ValidCode_CreatesASessionAndStoresOnlyTheHashOfItsId()
        {
            var sessionId = await SignInAsync();

            var stored = _store.Sessions.Should().ContainSingle().Subject;
            stored.IdHash.Should().NotBe(sessionId);
            stored.IdHash.Should().Be(SessionIds.Hash(sessionId));
            stored.AccessToken.Should().Be(FakeAuthorizationApi.JwtFor(1));
        }

        [TestMethod]
        public async Task CompleteLoginAsync_EachSignIn_GetsANewSessionId()
        {
            var first = await SignInAsync();
            var second = await SignInAsync();

            second.Should().NotBe(first);
        }

        [TestMethod]
        public async Task CompleteLoginAsync_TheSameLoginTwice_FailsTheSecondTime()
        {
            var start = await _sut.StartLoginAsync("/", TestContext.CancellationToken);
            await _sut.CompleteLoginAsync(start.LoginId, "one-time-code", null, TestContext.CancellationToken);

            var again = await _sut.CompleteLoginAsync(start.LoginId, "one-time-code", null, TestContext.CancellationToken);

            again.Should().BeNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("unknown-login")]
        public async Task CompleteLoginAsync_NoKnownLoginAttempt_FailsWithoutCallingTheApi(string? loginId)
        {
            var signedIn = await _sut.CompleteLoginAsync(loginId, "one-time-code", null, TestContext.CancellationToken);

            signedIn.Should().BeNull();
            _api.Exchanges.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CompleteLoginAsync_LoginAttemptTooOld_Fails()
        {
            var start = await _sut.StartLoginAsync("/", TestContext.CancellationToken);
            _time.Advance(TimeSpan.FromMinutes(_options.LoginTimeoutMinutes) + TimeSpan.FromSeconds(1));

            var signedIn = await _sut.CompleteLoginAsync(start.LoginId, "one-time-code", null, TestContext.CancellationToken);

            signedIn.Should().BeNull();
            _api.Exchanges.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CompleteLoginAsync_CodeRejectedByTheApi_FailsAndCreatesNoSession()
        {
            var start = await _sut.StartLoginAsync("/", TestContext.CancellationToken);

            var signedIn = await _sut.CompleteLoginAsync(start.LoginId, "forged-code", null, TestContext.CancellationToken);

            signedIn.Should().BeNull();
            _store.Sessions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task FindSessionAsync_KnownSession_ReturnsIt()
        {
            var sessionId = await SignInAsync();

            var session = await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken);

            session.Should().NotBeNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("forged")]
        public async Task FindSessionAsync_UnknownSession_ReturnsNull(string? sessionId)
        {
            var session = await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken);

            session.Should().BeNull();
        }

        [TestMethod]
        public async Task FindSessionAsync_IdleLongerThanTheTimeout_EndsTheSession()
        {
            var sessionId = await SignInAsync();
            _time.Advance(TimeSpan.FromMinutes(_options.IdleTimeoutMinutes) + TimeSpan.FromSeconds(1));

            var session = await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken);

            session.Should().BeNull();
            _store.Sessions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task FindSessionAsync_ActiveSessionPastTheAbsoluteLifetime_EndsTheSession()
        {
            var sessionId = await SignInAsync();
            var step = TimeSpan.FromMinutes(20);
            var absolute = TimeSpan.FromHours(_options.AbsoluteLifetimeHours);
            for (var elapsed = step; elapsed < absolute; elapsed += step)
            {
                _time.Advance(step);
                (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken)).Should().NotBeNull("the user keeps using the session");
            }

            _time.Advance(step);
            var session = await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken);

            session.Should().BeNull();
        }

        [TestMethod]
        public async Task FindSessionAsync_UsedBeforeTheIdleTimeout_SlidesTheTimeoutForward()
        {
            var sessionId = await SignInAsync();
            _time.Advance(TimeSpan.FromMinutes(_options.IdleTimeoutMinutes - 1));
            await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken);
            _time.Advance(TimeSpan.FromMinutes(_options.IdleTimeoutMinutes - 1));

            var session = await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken);

            session.Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_FreshToken_ReturnsItWithoutRefreshing()
        {
            var session = (await _sut.FindSessionAsync(await SignInAsync(), TestContext.CancellationToken))!;

            var token = await GetTokenAsync(session);

            token.Should().Be(FakeAuthorizationApi.JwtFor(1));
            _api.Refreshes.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_ExpiredToken_RefreshesAndStoresTheRotatedTokens()
        {
            var session = (await _sut.FindSessionAsync(await SignInAsync(), TestContext.CancellationToken))!;
            _time.Advance(TimeSpan.FromMinutes(16));

            var token = await GetTokenAsync(session);

            token.Should().Be(FakeAuthorizationApi.JwtFor(2));
            _api.Refreshes.Should().Equal("refresh-1");
            _store.Sessions.Single().RefreshToken.Should().Be("refresh-2");
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_TokenAboutToExpire_RefreshesBeforeItDoes()
        {
            var session = (await _sut.FindSessionAsync(await SignInAsync(), TestContext.CancellationToken))!;
            _time.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(_options.RefreshSkewSeconds / 2));

            await GetTokenAsync(session);

            _api.Refreshes.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_ManyRequestsFindAnExpiredToken_RefreshesOnlyOnce()
        {
            var sessionId = await SignInAsync();
            _time.Advance(TimeSpan.FromMinutes(16));
            _api.RefreshDelay = TimeSpan.FromMilliseconds(150);

            var tokens = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                var session = (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken))!;
                return await GetTokenAsync(session);
            }));

            _api.Refreshes.Should().HaveCount(1, "a second use of a refresh token would be read as theft and revoke the user's tokens");
            tokens.Should().OnlyContain(token => token == FakeAuthorizationApi.JwtFor(2));
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_RefreshTokenRejected_EndsTheSession()
        {
            var sessionId = await SignInAsync();
            var session = (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken))!;
            _time.Advance(TimeSpan.FromMinutes(16));
            _api.RejectRefresh = true;

            var token = await GetTokenAsync(session);

            token.Should().BeNull();
            (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken)).Should().BeNull();
        }

        [TestMethod]
        public async Task SignOutAsync_ActiveSession_RevokesTheRefreshTokenAndEndsTheSession()
        {
            var sessionId = await SignInAsync();
            var session = (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken))!;

            await _sut.SignOutAsync(session, TestContext.CancellationToken);

            _api.Logouts.Should().Equal("refresh-1");
            (await _sut.FindSessionAsync(sessionId, TestContext.CancellationToken)).Should().BeNull();
        }

        [TestMethod]
        public async Task GetProfile_Session_ReadsTheUserFromTheAccessTokenPayload()
        {
            var session = (await _sut.FindSessionAsync(await SignInAsync(), TestContext.CancellationToken))!;

            var profile = SessionService.TryReadProfile(session)!;

            profile.UserId.Should().Be("user-1");
            profile.Login.Should().Be("octocat");
            profile.AvatarUrl.Should().Be("https://avatars.test/u/1");
        }
    }
}
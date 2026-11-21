using Microsoft.Extensions.Options;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>The start of a sign-in: the cookie value that finds the verifier again, and the challenge for the Authorization API.</summary>
    public sealed record LoginStart(string LoginId, string CodeChallenge);

    /// <summary>A completed sign-in: the new session id for the cookie and the page to return to.</summary>
    public sealed record SignedIn(string SessionId, string ReturnUrl);

    /// <summary>What the SPA may know about the user, read from the access token payload; the token itself stays here.</summary>
    public sealed record UserProfile(string UserId, string? Login, string? AvatarUrl, string? Scope);

    /// <summary>Whether a session has a usable access token.</summary>
    public enum AccessTokenOutcome
    {
        /// <summary>The token is valid; use it.</summary>
        Available,

        /// <summary>The Authorization API rejected the refresh token, or the rotated tokens were lost: the session is over.</summary>
        SessionEnded,

        /// <summary>The token could not be renewed now (outage, timeout, a refresh that did not finish). The session is kept; try again later.</summary>
        Unavailable
    }

    /// <summary>The access token of a session, or why there is none.</summary>
    public sealed record AccessTokenResult(AccessTokenOutcome Outcome, string? Token = null);

    /// <summary>
    /// The browser session: sign-in with a PKCE-bound code, timeouts, access token refresh and sign-out.
    /// The tokens live in the store; the browser only holds the opaque session id.
    /// </summary>
    public sealed class SessionService(
        ISessionStore store,
        IAuthorizationApi authorizationApi,
        IOptions<BrowserSessionOptions> options,
        TimeProvider time,
        ILogger<SessionService> logger)
    {
        /// <summary>
        /// The Authorization API client times out well before the lease ends, so a slow refresh never outlives its lease.
        /// </summary>
        private static readonly TimeSpan RefreshLease = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RefreshWait = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);
        private const int SaveAttempts = 3;

        private readonly BrowserSessionOptions _options = options.Value;

        /// <summary>
        /// Starts a sign-in: keeps a fresh PKCE verifier (only its challenge leaves) and the local page to return to.
        /// </summary>
        public async Task<LoginStart> StartLoginAsync(string? returnUrl, CancellationToken cancellationToken)
        {
            var verifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
            var loginId = SessionIds.Create();

            await store.AddLoginAttemptAsync(new LoginAttempt
            {
                IdHash = SessionIds.Hash(loginId),
                CodeVerifier = verifier,
                ReturnUrl = LocalPathOrRoot(returnUrl),
                ExpiresAt = time.GetUtcNow().AddMinutes(_options.LoginTimeoutMinutes)
            });

            var challenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            return new LoginStart(loginId, challenge);
        }

        /// <summary>
        /// Completes a sign-in: exchanges the code with the verifier that this browser started the login with, then opens a new session.
        /// A new id is created every time and the session the browser held before (if any) is deleted, so an earlier session id
        /// never keeps working (no session fixation).
        /// </summary>
        public async Task<SignedIn?> CompleteLoginAsync(string? loginId, string? code, string? previousSessionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(loginId) || string.IsNullOrEmpty(code))
            {
                return null;
            }

            var attempt = await store.TakeLoginAttemptAsync(SessionIds.Hash(loginId));
            if (attempt is null || attempt.ExpiresAt < time.GetUtcNow())
            {
                logger.LogWarning("Sign-in completed without a known or current login attempt");
                return null;
            }

            var tokens = await authorizationApi.ExchangeCodeAsync(code, attempt.CodeVerifier, cancellationToken);
            if (tokens is null)
            {
                logger.LogWarning("The Authorization API rejected the one-time code");
                return null;
            }

            var sessionId = await OpenSessionAsync(tokens, previousSessionId);
            return new SignedIn(sessionId, attempt.ReturnUrl);
        }

        /// <summary>
        /// Opens a session for tokens the Authorization API issued (GitHub sign-in or admin passkey) and discards the session the browser
        /// held before, if any. The returned id is new every time, so an earlier id never keeps working.
        /// </summary>
        public async Task<string> OpenSessionAsync(IssuedTokens tokens, string? previousSessionId)
        {
            var now = time.GetUtcNow();
            var absoluteExpiresAt = now.AddHours(_options.AbsoluteLifetimeHours);
            var sessionId = SessionIds.Create();
            await store.AddSessionAsync(new Session
            {
                IdHash = SessionIds.Hash(sessionId),
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
                IsNewUser = tokens.IsNewUser,
                CreatedAt = now,
                LastSeenAt = now,
                AbsoluteExpiresAt = absoluteExpiresAt,
                ExpiresAt = Earlier(now.AddMinutes(_options.IdleTimeoutMinutes), absoluteExpiresAt)
            });

            if (!string.IsNullOrEmpty(previousSessionId))
            {
                await store.DeleteSessionAsync(SessionIds.Hash(previousSessionId));
            }

            return sessionId;
        }

        /// <summary>
        /// The session behind a cookie value, or null when it is unknown or past its idle or absolute limit (it is then removed).
        /// A found session slides its idle timeout forward.
        /// </summary>
        public async Task<Session?> FindSessionAsync(string? sessionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            var idHash = SessionIds.Hash(sessionId);
            var session = await store.FindSessionAsync(idHash);
            if (session is null)
            {
                return null;
            }

            var now = time.GetUtcNow();
            var idleLimit = session.LastSeenAt.AddMinutes(_options.IdleTimeoutMinutes);
            if (now >= idleLimit || now >= session.AbsoluteExpiresAt)
            {
                await store.DeleteSessionAsync(idHash);
                return null;
            }

            if (now - session.LastSeenAt >= TouchInterval)
            {
                var expiresAt = Earlier(now.AddMinutes(_options.IdleTimeoutMinutes), session.AbsoluteExpiresAt);
                await store.TouchAsync(idHash, now, expiresAt);
                session.LastSeenAt = now;
                session.ExpiresAt = expiresAt;
            }

            return session;
        }

        /// <summary>
        /// A valid access token for the session, refreshed when it is expired or about to be. Only one request at a time
        /// may use the refresh token: the others wait and take the result, because a refresh token used twice is treated as stolen.
        /// </summary>
        public async Task<AccessTokenResult> GetAccessTokenAsync(Session session, CancellationToken cancellationToken)
        {
            var deadline = time.GetUtcNow() + RefreshWait;
            var current = session;

            while (true)
            {
                if (!NeedsRefresh(current))
                {
                    return new AccessTokenResult(AccessTokenOutcome.Available, current.AccessToken);
                }

                var leaseId = await store.TryAcquireRefreshLeaseAsync(current.IdHash, time.GetUtcNow(), time.GetUtcNow() + RefreshLease);
                if (leaseId is not null)
                {
                    return await RefreshAsync(current.IdHash, leaseId, cancellationToken);
                }

                if (time.GetUtcNow() > deadline)
                {
                    logger.LogWarning("Gave up waiting for another request to refresh the session");
                    return new AccessTokenResult(AccessTokenOutcome.Unavailable);
                }

                await Task.Delay(50, cancellationToken);
                var reloaded = await store.FindSessionAsync(current.IdHash);
                if (reloaded is null)
                {
                    return new AccessTokenResult(AccessTokenOutcome.SessionEnded);
                }

                current = reloaded;
            }
        }

        /// <summary>
        /// Ends the session and revokes its refresh token. A failure to reach the Authorization API does not keep the user signed in.
        /// </summary>
        public async Task SignOutAsync(Session session, CancellationToken cancellationToken)
        {
            try
            {
                await authorizationApi.LogoutAsync(session.RefreshToken, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
            {
                logger.LogWarning(exception, "The refresh token could not be revoked at sign-out");
            }
            finally
            {
                await store.DeleteSessionAsync(session.IdHash);
            }
        }

        /// <summary>
        /// Whether the user counts as new: the first sign-in, shortly after it happened.
        /// </summary>
        public bool IsNewUser(Session session) =>
            session.IsNewUser && time.GetUtcNow() < session.CreatedAt.AddMinutes(_options.NewUserMinutes);

        /// <summary>
        /// Reads the user from the access token payload, or null when the token cannot be read.
        /// The token came straight from the Authorization API, so it is not verified again here.
        /// </summary>
        public static UserProfile? TryReadProfile(Session session) => TryReadAccessTokenProfile(session.AccessToken);

        /// <summary>
        /// Reads the user from an access token payload, or null when it cannot be read.
        /// </summary>
        public static UserProfile? TryReadAccessTokenProfile(string accessToken)
        {
            try
            {
                var parts = accessToken.Split('.');
                using var payload = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
                var root = payload.RootElement;
                string? Read(string name) => root.TryGetProperty(name, out var value) ? value.GetString() : null;
                return new UserProfile(Read("sub") ?? string.Empty, Read("login"), Read("avatar_url"), Read("scope"));
            }
            catch (Exception exception) when (exception is JsonException or FormatException or IndexOutOfRangeException or InvalidOperationException)
            {
                return null;
            }
        }

        private async Task<AccessTokenResult> RefreshAsync(string idHash, string leaseId, CancellationToken cancellationToken)
        {
            try
            {
                // Another request may have refreshed between the check and the lease.
                var latest = await store.FindSessionAsync(idHash);
                if (latest is null)
                {
                    return new AccessTokenResult(AccessTokenOutcome.SessionEnded);
                }

                if (!NeedsRefresh(latest))
                {
                    return new AccessTokenResult(AccessTokenOutcome.Available, latest.AccessToken);
                }

                IssuedTokens? tokens;
                try
                {
                    tokens = await authorizationApi.RefreshAsync(latest.RefreshToken, cancellationToken);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
                {
                    logger.LogWarning(exception, "The access token could not be refreshed now; the session is kept");
                    return new AccessTokenResult(AccessTokenOutcome.Unavailable);
                }

                if (tokens is null)
                {
                    logger.LogInformation("The refresh token was rejected; the session ends");
                    await store.DeleteSessionAsync(idHash);
                    return new AccessTokenResult(AccessTokenOutcome.SessionEnded);
                }

                return await SaveRotatedTokensAsync(idHash, leaseId, tokens);
            }
            finally
            {
                // A no-op when the tokens were saved (that releases the lease) or when the lease was taken over.
                await ReleaseQuietlyAsync(idHash, leaseId);
            }
        }

        /// <summary>
        /// The Authorization API has already rotated the refresh token, so these tokens are the only usable ones. Losing them would
        /// make the next refresh reuse the old token, which counts as theft and revokes every token of the user.
        /// </summary>
        private async Task<AccessTokenResult> SaveRotatedTokensAsync(string idHash, string leaseId, IssuedTokens tokens)
        {
            for (var attempt = 1; attempt <= SaveAttempts; attempt++)
            {
                try
                {
                    var saved = await store.SaveTokensAsync(idHash, leaseId, tokens);
                    if (saved)
                    {
                        return new AccessTokenResult(AccessTokenOutcome.Available, tokens.AccessToken);
                    }

                    logger.LogWarning("The refresh lease was taken over before the rotated tokens could be saved; the session ends");
                    break;
                }
                catch (Exception exception) when (attempt < SaveAttempts)
                {
                    logger.LogWarning(exception, "Saving the rotated tokens failed (attempt {Attempt})", attempt);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Saving the rotated tokens failed; the session ends");
                    break;
                }
            }

            try
            {
                await store.DeleteSessionAsync(idHash);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The session could not be deleted after the rotated tokens were lost");
            }

            return new AccessTokenResult(AccessTokenOutcome.SessionEnded);
        }

        private async Task ReleaseQuietlyAsync(string idHash, string leaseId)
        {
            try
            {
                await store.ReleaseRefreshLeaseAsync(idHash, leaseId);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "The refresh lease could not be released; it expires on its own");
            }
        }

        private bool NeedsRefresh(Session session) =>
            session.AccessTokenExpiresAt <= time.GetUtcNow().AddSeconds(_options.RefreshSkewSeconds);

        private static DateTimeOffset Earlier(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

        /// <summary>
        /// A local path only: it starts with a single slash and holds no control character or backslash, which browsers
        /// strip or turn into a slash ("/&lt;TAB&gt;/evil.test" would become "//evil.test").
        /// </summary>
        private static string LocalPathOrRoot(string? returnUrl)
        {
            var isLocalPath = !string.IsNullOrEmpty(returnUrl)
                && returnUrl[0] == '/'
                && (returnUrl.Length == 1 || returnUrl[1] != '/')
                && returnUrl.All(character => character >= ' ' && character != '\u007F' && character != '\\');
            return isLocalPath ? returnUrl! : "/";
        }
    }
}
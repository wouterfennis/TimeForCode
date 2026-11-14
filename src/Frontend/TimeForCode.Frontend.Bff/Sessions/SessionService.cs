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
        private static readonly TimeSpan RefreshLease = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan RefreshWait = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);

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
        /// A new id is created every time, so an earlier session id is never reused (no session fixation).
        /// </summary>
        public async Task<SignedIn?> CompleteLoginAsync(string? loginId, string? code, CancellationToken cancellationToken)
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

            return new SignedIn(sessionId, attempt.ReturnUrl);
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
        /// Null when the session ended (the Authorization API rejected the refresh token).
        /// </summary>
        public async Task<string?> GetAccessTokenAsync(Session session, CancellationToken cancellationToken)
        {
            var deadline = time.GetUtcNow() + RefreshWait;
            var current = session;

            while (true)
            {
                if (!NeedsRefresh(current))
                {
                    return current.AccessToken;
                }

                if (await store.TryAcquireRefreshLeaseAsync(current.IdHash, time.GetUtcNow(), time.GetUtcNow() + RefreshLease))
                {
                    return await RefreshAsync(current.IdHash, cancellationToken);
                }

                if (time.GetUtcNow() > deadline)
                {
                    logger.LogWarning("Gave up waiting for another request to refresh the session");
                    return null;
                }

                await Task.Delay(50, cancellationToken);
                var reloaded = await store.FindSessionAsync(current.IdHash);
                if (reloaded is null)
                {
                    return null;
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
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "The refresh token could not be revoked at sign-out");
            }

            await store.DeleteSessionAsync(session.IdHash);
        }

        /// <summary>
        /// Whether the user counts as new: the first sign-in, shortly after it happened.
        /// </summary>
        public bool IsNewUser(Session session) =>
            session.IsNewUser && time.GetUtcNow() < session.CreatedAt.AddMinutes(_options.NewUserMinutes);

        /// <summary>
        /// Reads the user from the access token payload. The token came straight from the Authorization API, so it is not verified again here.
        /// </summary>
        public static UserProfile ReadProfile(Session session)
        {
            var parts = session.AccessToken.Split('.');
            using var payload = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
            var root = payload.RootElement;
            string? Read(string name) => root.TryGetProperty(name, out var value) ? value.GetString() : null;
            return new UserProfile(Read("sub") ?? string.Empty, Read("login"), Read("avatar_url"), Read("scope"));
        }

        private async Task<string?> RefreshAsync(string idHash, CancellationToken cancellationToken)
        {
            try
            {
                // Another request may have refreshed between the check and the lease.
                var latest = await store.FindSessionAsync(idHash);
                if (latest is null)
                {
                    return null;
                }

                if (!NeedsRefresh(latest))
                {
                    await store.ReleaseRefreshLeaseAsync(idHash);
                    return latest.AccessToken;
                }

                var tokens = await authorizationApi.RefreshAsync(latest.RefreshToken, cancellationToken);
                if (tokens is null)
                {
                    logger.LogInformation("The refresh token was rejected; the session ends");
                    await store.DeleteSessionAsync(idHash);
                    return null;
                }

                await store.SaveTokensAsync(idHash, tokens);
                return tokens.AccessToken;
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "The Authorization API could not be reached to refresh the access token");
                await store.ReleaseRefreshLeaseAsync(idHash);
                return null;
            }
        }

        private bool NeedsRefresh(Session session) =>
            session.AccessTokenExpiresAt <= time.GetUtcNow().AddSeconds(_options.RefreshSkewSeconds);

        private static DateTimeOffset Earlier(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

        private static string LocalPathOrRoot(string? returnUrl)
        {
            var isLocalPath = !string.IsNullOrEmpty(returnUrl)
                && returnUrl[0] == '/'
                && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'));
            return isLocalPath ? returnUrl! : "/";
        }
    }
}
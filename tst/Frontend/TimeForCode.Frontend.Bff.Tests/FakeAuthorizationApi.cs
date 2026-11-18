using TimeForCode.Frontend.Bff.Sessions;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>Stands in for the Authorization API's token endpoints; records what the BFF sent.</summary>
    internal sealed class FakeAuthorizationApi(TimeProvider time) : IAuthorizationApi
    {
        private int _issued;

        public List<(string Code, string Verifier)> Exchanges { get; } = [];

        public List<string> Refreshes { get; } = [];

        public List<string> Logouts { get; } = [];

        public string? ValidCode { get; set; } = "one-time-code";

        public string? ValidVerifier { get; set; }

        public bool RejectRefresh { get; set; }

        public bool IsNewUser { get; set; }

        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

        public TimeSpan RefreshDelay { get; set; } = TimeSpan.Zero;

        public Exception? RefreshFailure { get; set; }

        public Exception? LogoutFailure { get; set; }

        public Exception? ExchangeFailure { get; set; }

        public Exception? AdminFailure { get; set; }

        public Task<IssuedTokens?> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
        {
            Exchanges.Add((code, codeVerifier));
            if (ExchangeFailure is not null)
            {
                throw ExchangeFailure;
            }

            var accepted = code == ValidCode && (ValidVerifier is null || codeVerifier == ValidVerifier);
            return Task.FromResult(accepted ? Issue(IsNewUser) : null);
        }

        public async Task<IssuedTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            Refreshes.Add(refreshToken);
            if (RefreshFailure is not null)
            {
                throw RefreshFailure;
            }

            if (RefreshDelay > TimeSpan.Zero)
            {
                await Task.Delay(RefreshDelay, cancellationToken);
            }

            return RejectRefresh ? null : Issue(isNewUser: false);
        }

        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            Logouts.Add(refreshToken);
            if (LogoutFailure is not null)
            {
                throw LogoutFailure;
            }

            return Task.CompletedTask;
        }

        public List<(string Secret, string Credential, string CeremonyId)> AdminRegistrations { get; } = [];

        public List<(string Credential, string CeremonyId)> AdminAuthentications { get; } = [];

        public string AdminBootstrapSecret { get; set; } = "bootstrap-secret";

        public bool AdminRegistered { get; set; } = true;

        public bool AdminTokenIsUnreadable { get; set; }

        public HashSet<string> UsedCeremonies { get; } = [];

        public Task<AdminCeremony?> AdminRegistrationOptionsAsync(string bootstrapSecret, CancellationToken cancellationToken) =>
            AdminFailure is not null ? throw AdminFailure : Task.FromResult(bootstrapSecret == AdminBootstrapSecret && !AdminRegistered ? new AdminCeremony("registration-ceremony", "{\"challenge\":\"r\"}") : null);

        public Task<IssuedTokens?> AdminCompleteRegistrationAsync(string bootstrapSecret, string credentialJson, string ceremonyId, CancellationToken cancellationToken)
        {
            AdminRegistrations.Add((bootstrapSecret, credentialJson, ceremonyId));
            var accepted = bootstrapSecret == AdminBootstrapSecret && !AdminRegistered && UsedCeremonies.Add(ceremonyId);
            return Task.FromResult(accepted ? IssueAdmin() : null);
        }

        public Task<AdminCeremony?> AdminAuthenticationOptionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(AdminRegistered ? new AdminCeremony("authentication-ceremony", "{\"challenge\":\"a\"}") : null);

        public Task<IssuedTokens?> AdminCompleteAuthenticationAsync(string credentialJson, string ceremonyId, CancellationToken cancellationToken)
        {
            AdminAuthentications.Add((credentialJson, ceremonyId));
            var accepted = AdminRegistered && credentialJson != "wrong-device" && UsedCeremonies.Add(ceremonyId);
            return Task.FromResult(accepted ? IssueAdmin() : null);
        }

        public IssuedTokens IssueAdmin()
        {
            var n = Interlocked.Increment(ref _issued);
            var now = time.GetUtcNow();
            return new IssuedTokens(AdminTokenIsUnreadable ? "garbage" : JwtFor(n, "admin", "admin"), now.Add(AccessTokenLifetime), $"refresh-{n}", now.AddDays(7), IsNewUser: false);
        }

        public IssuedTokens Issue(bool isNewUser)
        {
            var n = Interlocked.Increment(ref _issued);
            var now = time.GetUtcNow();
            return new IssuedTokens(
                AccessToken: JwtFor(n),
                AccessTokenExpiresAt: now.Add(AccessTokenLifetime),
                RefreshToken: $"refresh-{n}",
                RefreshTokenExpiresAt: now.AddDays(7),
                IsNewUser: isNewUser);
        }

        /// <summary>An unsigned JWT shaped like the platform's: the BFF reads only the payload.</summary>
        public static string JwtFor(int n, string sub = "user-1", string scope = "user")
        {
            static string B64(string json) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{B64("{\"alg\":\"none\"}")}.{B64($"{{\"sub\":\"{sub}\",\"login\":\"octocat\",\"avatar_url\":\"https://avatars.test/u/1\",\"scope\":\"{scope}\",\"n\":{n}}}")}.sig";
        }
    }
}
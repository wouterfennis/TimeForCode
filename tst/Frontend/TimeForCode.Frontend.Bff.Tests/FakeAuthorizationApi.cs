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

        public Task<IssuedTokens?> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
        {
            Exchanges.Add((code, codeVerifier));
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
        public static string JwtFor(int n)
        {
            static string B64(string json) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{B64("{\"alg\":\"none\"}")}.{B64($"{{\"sub\":\"user-1\",\"login\":\"octocat\",\"avatar_url\":\"https://avatars.test/u/1\",\"scope\":\"user\",\"n\":{n}}}")}.sig";
        }
    }
}
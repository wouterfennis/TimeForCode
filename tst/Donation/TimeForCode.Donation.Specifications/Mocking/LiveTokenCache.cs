using System.IdentityModel.Tokens.Jwt;

namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>Keeps a JWT until shortly before it expires and never keeps a failed acquisition.</summary>
    internal sealed class LiveTokenCache
    {
        private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(1);

        private readonly Func<Task<string>> _acquire;
        private readonly Func<DateTime> _utcNow;
        private readonly object _gate = new();
        private Task<string>? _token;

        public LiveTokenCache(Func<Task<string>> acquire, Func<DateTime>? utcNow = null)
        {
            _acquire = acquire;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public Task<string> GetAsync()
        {
            lock (_gate)
            {
                if (_token is null || IsFailed(_token) || IsExpiring(_token))
                {
                    _token = _acquire();
                }

                return _token;
            }
        }

        private static bool IsFailed(Task<string> token) => token.IsFaulted || token.IsCanceled;

        private bool IsExpiring(Task<string> token)
        {
            return token.IsCompletedSuccessfully
                && new JwtSecurityTokenHandler().ReadJwtToken(token.Result).ValidTo - _utcNow() < ExpiryMargin;
        }
    }
}
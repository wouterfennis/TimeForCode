using System.IdentityModel.Tokens.Jwt;

namespace TimeForCode.Shared.Live
{
    /// <summary>Keeps a JWT until shortly before it expires and never keeps a failed or unreadable acquisition.</summary>
    public sealed class LiveTokenCache
    {
        private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(1);

        private readonly Func<Task<string>> _acquire;
        private readonly Func<DateTime> _utcNow;
        private readonly object _gate = new();
        private Task<string>? _token;
        private DateTime _validTo;

        public LiveTokenCache(Func<Task<string>> acquire, Func<DateTime>? utcNow = null)
        {
            _acquire = acquire;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public Task<string> GetAsync()
        {
            lock (_gate)
            {
                if (_token is null || _token.IsFaulted || _token.IsCanceled || IsExpiring(_token))
                {
                    _token = AcquireAsync();
                }

                return _token;
            }
        }

        // The expiry is read once, so a token that is not a readable JWT faults the task and is not kept.
        private async Task<string> AcquireAsync()
        {
            string token = await _acquire();
            _validTo = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo;
            return token;
        }

        private bool IsExpiring(Task<string> token)
        {
            return token.IsCompletedSuccessfully && _validTo - _utcNow() < ExpiryMargin;
        }
    }
}
namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// A clock that always returns the time it was given, so scenarios can assert exact timestamps.
    /// </summary>
    internal sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
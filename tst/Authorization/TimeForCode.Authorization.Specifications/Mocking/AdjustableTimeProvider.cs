namespace TimeForCode.Authorization.Specifications.Mocking
{
    public class AdjustableTimeProvider : TimeProvider
    {
        private TimeSpan _offset = TimeSpan.Zero;

        public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + _offset;

        public void Advance(TimeSpan duration) => _offset += duration;
    }
}
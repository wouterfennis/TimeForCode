namespace TimeForCode.Reporting.Specifications.Mocking
{
    /// <summary>
    /// The limit of the "public" rate-limit policy of the shared spec host. It is unlimited, so one host can serve every scenario;
    /// a scenario that needs the limit lowers it. Every reset starts a new window, so a lowered limit never reaches the next scenario.
    /// </summary>
    internal class PublicRateLimit
    {
        internal int PermitLimit { get; private set; } = int.MaxValue;

        internal int Window { get; private set; }

        internal void Lower(int permitLimit)
        {
            PermitLimit = permitLimit;
        }

        internal void Reset()
        {
            PermitLimit = int.MaxValue;
            Window++;
        }
    }
}
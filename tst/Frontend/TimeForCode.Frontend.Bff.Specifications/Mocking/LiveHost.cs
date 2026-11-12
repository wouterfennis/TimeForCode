namespace TimeForCode.Frontend.Bff.Specifications.Mocking
{
    /// <summary>
    /// Scenarios tagged <c>@live</c> run against the front-end of a running compose stack and are skipped without one.
    /// </summary>
    internal static class LiveHost
    {
        public const string BaseUrlVariable = "TIMEFORCODE_LIVE_FRONTEND_URL";
        public const string Tag = "live";

        public static Uri? ConfiguredBaseAddress
        {
            get
            {
                string? configured = Environment.GetEnvironmentVariable(BaseUrlVariable);
                return string.IsNullOrWhiteSpace(configured) ? null : new Uri(configured, UriKind.Absolute);
            }
        }
    }
}
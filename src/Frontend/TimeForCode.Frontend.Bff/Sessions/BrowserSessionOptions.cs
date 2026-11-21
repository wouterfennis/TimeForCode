namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// Settings of the browser session (configuration section <c>Session</c>).
    /// </summary>
    public sealed class BrowserSessionOptions
    {
        public const string SectionName = "Session";

        /// <summary>
        /// The session ends after this many minutes without a request.
        /// </summary>
        public int IdleTimeoutMinutes { get; init; } = 30;

        /// <summary>
        /// The session ends this many hours after sign-in, however active it is.
        /// </summary>
        public int AbsoluteLifetimeHours { get; init; } = 8;

        /// <summary>
        /// An access token is refreshed when it expires within this many seconds.
        /// </summary>
        public int RefreshSkewSeconds { get; init; } = 30;

        /// <summary>
        /// How long a started sign-in can be completed, in minutes.
        /// </summary>
        public int LoginTimeoutMinutes { get; init; } = 10;

        /// <summary>
        /// The user counts as new for this many minutes after the first sign-in, so the SPA can show the first-login welcome once.
        /// </summary>
        public int NewUserMinutes { get; init; } = 5;

        /// <summary>
        /// How many sign-in requests (start, callback, admin passkey ceremonies) one client address may make per minute.
        /// </summary>
        public int SignInRequestsPerMinute { get; init; } = 40;
    }
}
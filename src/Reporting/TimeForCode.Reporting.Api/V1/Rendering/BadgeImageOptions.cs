namespace TimeForCode.Reporting.Api.V1.Rendering
{
    /// <summary>
    /// How long badge images may be cached by others, such as the image proxy of GitHub.
    /// </summary>
    public class BadgeImageOptions
    {
        /// <summary>The configuration section the options are read from.</summary>
        public const string SectionName = nameof(BadgeImageOptions);

        /// <summary>Seconds a badge with a value, "no badge yet" or "not found" may be cached.</summary>
        public int CacheSeconds { get; set; } = 300;

        /// <summary>Seconds an "unavailable" badge may be cached; short, so the badge recovers soon after the Donation API does.</summary>
        public int UnavailableCacheSeconds { get; set; } = 30;
    }
}
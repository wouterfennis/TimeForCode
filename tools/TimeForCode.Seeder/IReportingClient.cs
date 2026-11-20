namespace TimeForCode.Seeder
{
    /// <summary>The part of the Reporting API the seeder uses. Reporting has no generated client, so this stays a small hand-written one.</summary>
    internal interface IReportingClient
    {
        /// <summary>Defines (or updates) a badge tier of a recipient type, "organization" or "contributor". Needs an administrator.</summary>
        Task DefineBadgeTierAsync(string recipientType, string name, int thresholdHours);
    }
}
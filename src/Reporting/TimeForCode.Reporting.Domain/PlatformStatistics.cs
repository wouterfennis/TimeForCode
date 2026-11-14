namespace TimeForCode.Reporting.Domain
{
    /// <summary>
    /// Platform-wide totals as read from the Donation context; no personal data.
    /// </summary>
    public record PlatformStatistics(long TotalHoursLogged, int OrganizationsSupported);
}
namespace TimeForCode.Reporting.Values
{
    /// <summary>
    /// Lifecycle state of a donation as the Donation context reports it; duplicated here on purpose (ADR-010).
    /// </summary>
    public enum DonationStatus
    {
        Pending,
        Active,
        Completed,
        Cancelled
    }
}
namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// Lifecycle state of a donation pledge. The numeric values are persisted, so existing values must not change.
    /// </summary>
    public enum DonationStatus
    {
        /// <summary>The pledge was made and the project maintainer has not accepted it yet.</summary>
        Pending = 0,

        /// <summary>The pledge was accepted and the committed hours are being worked.</summary>
        Active = 1,

        /// <summary>All committed hours were worked and the donation is finished.</summary>
        Completed = 2,

        /// <summary>The donation was cancelled before it was completed.</summary>
        Cancelled = 3
    }
}
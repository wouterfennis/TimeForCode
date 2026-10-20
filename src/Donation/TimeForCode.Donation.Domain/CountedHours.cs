namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// The hours a donation took when logged hours were counted toward it.
    /// </summary>
    public record CountedHours(Donation Donation, int Hours);
}
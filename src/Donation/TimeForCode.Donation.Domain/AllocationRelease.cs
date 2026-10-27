namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// Part of a release plan: how many hours to take back from one allocation.
    /// </summary>
    public sealed record AllocationRelease(DonationAllocation Allocation, int Hours)
    {
        /// <summary>The allocation as it is left after the release, or <c>null</c> when all its hours are released and it goes away.</summary>
        public DonationAllocation? Remaining => Hours == Allocation.Hours ? null : Allocation.Reduce(Hours);
    }
}
namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// Limits of a logged contribution, shared by the domain, the commands and the API.
    /// </summary>
    public static class ContributionLimits
    {
        public const int MaxHours = 10_000;
        public const int MaxReferenceLength = 2048;
    }
}
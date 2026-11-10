namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// Limits of a logged contribution, shared by the domain, the commands and the API.
    /// </summary>
    public static class ContributionLimits
    {
        /// <summary>The most hours a single contribution can record.</summary>
        public const int MaxHours = 10_000;

        /// <summary>The longest GitHub reference, in characters.</summary>
        public const int MaxReferenceLength = 2048;
    }
}
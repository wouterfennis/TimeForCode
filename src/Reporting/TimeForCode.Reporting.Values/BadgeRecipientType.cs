namespace TimeForCode.Reporting.Values
{
    /// <summary>
    /// Who a badge tier is meant for; each recipient type has its own, independent set of tiers.
    /// </summary>
    public enum BadgeRecipientType
    {
        /// <summary>A donor organization, measured on the hours logged towards its donations.</summary>
        Organization,
        /// <summary>A contributor, measured on the total hours they logged themselves.</summary>
        Contributor
    }
}
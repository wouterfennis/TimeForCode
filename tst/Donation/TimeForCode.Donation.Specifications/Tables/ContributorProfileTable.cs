namespace TimeForCode.Donation.Specifications.Tables
{
    /// <summary>The columns of the contributor profile tables.</summary>
    internal sealed class ContributorProfileTable
    {
        public string? Role { get; set; }

        public string? Skills { get; set; }

        public int? HoursAvailable { get; set; }

        public string? DonorOrganization { get; set; }
    }
}
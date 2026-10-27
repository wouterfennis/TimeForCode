namespace TimeForCode.Donation.Specifications.Tables
{
    /// <summary>The columns of the project search table.</summary>
    internal sealed class SearchProjectsTable
    {
        public string? Text { get; set; }

        public string? Language { get; set; }

        public string? Tag { get; set; }

        public int? Page { get; set; }

        public int? PageSize { get; set; }
    }
}
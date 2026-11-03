namespace TimeForCode.Seeder.Scenarios
{
    /// <summary>Keys under which scenarios hand the ids they created to later scenarios.</summary>
    internal static class SeedIds
    {
        public const string AcmeOrganization = "organization.acme";
        public const string GlobexOrganization = "organization.globex";

        public const string DraftProject = "project.draft";
        public const string PendingProject = "project.pending";
        public const string ApprovedProject = "project.approved";
        public const string SecondApprovedProject = "project.approved-second";
        public const string ChangesRequestedProject = "project.changes-requested";
        public const string ArchivedProject = "project.archived";
    }
}
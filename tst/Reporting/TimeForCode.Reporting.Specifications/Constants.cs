namespace TimeForCode.Reporting.Specifications
{
    internal static class Constants
    {
        internal const string TestUserId = "507f1f77bcf86cd799439011";
        internal const string TestDonorOrganizationId = "507f1f77bcf86cd799439013";
        internal const string TestProjectId = "507f1f77bcf86cd799439031";
        internal const string TestJwtSigningKey = "reporting-specs-signing-key-for-tests-only";
        internal const string ContributionHistoryCsvHeader = "ContributionId,ProjectId,ContributorLogin,Hours,GithubReference,LoggedAt";

        /// <summary>A deterministic 24 character hexadecimal identifier for a contributor login, the shape of the identifiers the Donation context issues.</summary>
        internal static string ContributorIdOf(string login) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(login)))[..24].ToLowerInvariant();
    }
}
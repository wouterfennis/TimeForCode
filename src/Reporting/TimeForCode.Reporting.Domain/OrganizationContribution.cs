namespace TimeForCode.Reporting.Domain
{
    /// <summary>
    /// Hours logged by a contributor of a donor organization, as read from the Donation context.
    /// </summary>
    public record OrganizationContribution(string Id, string ProjectId, string ContributorLogin, int Hours, string? GithubReference, DateTimeOffset LoggedAt);
}
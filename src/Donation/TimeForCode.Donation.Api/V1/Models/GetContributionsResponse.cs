namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents the contributions logged on a project.
    /// </summary>
    public class GetContributionsResponse
    {
        /// <summary>The contributions, earliest first.</summary>
        public required IReadOnlyList<ContributionResponse> Contributions { get; init; }
    }
}
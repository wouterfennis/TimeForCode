namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents the profile of a contributor.
    /// </summary>
    public class ContributorProfileResponse
    {
        /// <summary>The contributor identifier.</summary>
        public required string Id { get; init; }

        /// <summary>The GitHub login of the contributor.</summary>
        public required string GithubLogin { get; init; }

        /// <summary>The avatar URL of the contributor.</summary>
        public required string AvatarUrl { get; init; }

        /// <summary>The role of the contributor; absent when not set.</summary>
        public string? Role { get; init; }

        /// <summary>The normalised skills of the contributor; empty when none were set.</summary>
        public required IReadOnlyList<string> Skills { get; init; }

        /// <summary>The whole number of hours the contributor can donate; absent when not set.</summary>
        public int? HoursAvailable { get; init; }

        /// <summary>The identifier of the affiliated donor organization; absent when independent.</summary>
        public string? DonorOrganizationId { get; init; }
    }
}
namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents the profile of a maintainer, the owner of one or more projects.
    /// </summary>
    public class MaintainerProfileResponse
    {
        /// <summary>The maintainer identifier.</summary>
        public required string Id { get; init; }

        /// <summary>The GitHub login of the maintainer.</summary>
        public required string GithubLogin { get; init; }

        /// <summary>The avatar URL of the maintainer.</summary>
        public required string AvatarUrl { get; init; }
    }
}
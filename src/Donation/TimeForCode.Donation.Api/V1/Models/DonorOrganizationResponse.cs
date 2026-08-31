namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a donor organization response.
    /// </summary>
    public class DonorOrganizationResponse
    {
        /// <summary>
        /// The unique identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// The name of the donor organization.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// The contact email of the donor organization.
        /// </summary>
        public string? ContactEmail { get; init; }

        /// <summary>
        /// The website URL of the donor organization.
        /// </summary>
        public string? Website { get; init; }

        /// <summary>
        /// When the donor organization was created.
        /// </summary>
        public required DateTimeOffset CreatedAt { get; init; }

        /// <summary>
        /// When the donor organization was last updated.
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
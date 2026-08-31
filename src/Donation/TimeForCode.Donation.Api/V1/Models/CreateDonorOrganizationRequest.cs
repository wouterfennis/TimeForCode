namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a request to create a donor organization.
    /// </summary>
    public class CreateDonorOrganizationRequest
    {
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
    }
}
using System.ComponentModel.DataAnnotations;

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
        /// <example>Acme Software</example>
        [StringLength(ApiConstraints.MaxOrganizationNameLength, MinimumLength = 1)]
        public required string Name { get; init; }

        /// <summary>
        /// The contact email of the donor organization.
        /// </summary>
        /// <example>open-source@acme.example</example>
        [EmailAddress]
        [StringLength(ApiConstraints.MaxEmailLength)]
        public string? ContactEmail { get; init; }

        /// <summary>
        /// The website URL of the donor organization.
        /// </summary>
        /// <example>https://acme.example</example>
        [Url]
        [StringLength(ApiConstraints.MaxUrlLength)]
        public string? Website { get; init; }
    }
}
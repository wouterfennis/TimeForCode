using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a pledge of developer hours to a project. Exactly one donor must be given: an individual or a donor organization.
    /// </summary>
    public class PledgeDonationRequest
    {
        /// <summary>
        /// The whole number of hours pledged.
        /// </summary>
        [Range(1, ApiConstraints.MaxDonationHours)]
        public required int Hours { get; init; }

        /// <summary>
        /// Pledge as an individual. Exclusive with <see cref="DonorOrganizationId"/>.
        /// </summary>
        public required bool AsIndividual { get; init; }

        /// <summary>
        /// The donor organization the hours are pledged for. Exclusive with <see cref="AsIndividual"/>.
        /// </summary>
        [ObjectId]
        public string? DonorOrganizationId { get; init; }
    }
}
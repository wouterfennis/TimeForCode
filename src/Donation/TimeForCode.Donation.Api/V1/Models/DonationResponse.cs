using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a donation pledge and its progress.
    /// </summary>
    public class DonationResponse
    {
        /// <summary>The donation identifier.</summary>
        public required string Id { get; init; }

        /// <summary>The identifier of the project the hours are pledged to.</summary>
        public required string ProjectId { get; init; }

        /// <summary>The donor organization of the pledge; absent when pledged as an individual.</summary>
        public string? DonorOrganizationId { get; init; }

        /// <summary>The lifecycle state: Pending, Active, Completed or Cancelled.</summary>
        public required DonationStatus Status { get; init; }

        /// <summary>The number of hours pledged.</summary>
        public required int HoursCommitted { get; init; }

        /// <summary>The number of pledged hours that were logged on the project while the donation was active.</summary>
        public required int HoursLogged { get; init; }

        /// <summary>The share of committed hours that is logged, in whole percent (0 to 100).</summary>
        public required int ProgressPercentage { get; init; }

        /// <summary>The moment the pledge was made.</summary>
        public required DateTimeOffset CreatedAt { get; init; }

        /// <summary>The moment the pledge last changed.</summary>
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
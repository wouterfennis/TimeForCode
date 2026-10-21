using System.ComponentModel.DataAnnotations;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents a request to replace a contributor profile; fields that are left out are cleared.
    /// </summary>
    public class UpdateContributorProfileRequest
    {
        /// <summary>
        /// The role of the contributor.
        /// </summary>
        /// <example>Backend developer</example>
        [StringLength(ContributorProfileLimits.MaxRoleLength)]
        public string? Role { get; init; }

        /// <summary>
        /// The skills of the contributor; each is trimmed, blank ones are dropped and duplicates (ignoring case) are merged.
        /// Each skill has at most 100 characters.
        /// </summary>
        /// <example>["C#", "MongoDB"]</example>
        [MaxLength(ContributorProfileLimits.MaxSkills)]
        public List<string> Skills { get; init; } = [];

        /// <summary>
        /// The whole number of hours the contributor can donate.
        /// </summary>
        /// <example>10</example>
        [Range(0, ContributorProfileLimits.MaxHoursAvailable)]
        public int? HoursAvailable { get; init; }

        /// <summary>
        /// The identifier of the donor organization the contributor is affiliated with; the organization must exist.
        /// </summary>
        [RegularExpression(ApiConstraints.ObjectIdPattern)]
        public string? DonorOrganizationId { get; init; }
    }
}
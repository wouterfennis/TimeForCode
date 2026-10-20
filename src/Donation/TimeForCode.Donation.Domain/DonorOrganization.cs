using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Domain
{
    public class DonorOrganization : DocumentEntity
    {
        public required string Name { get; set; }
        public string? ContactEmail { get; set; }
        public string? Website { get; set; }
        public required DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public List<Contributor> EmployeeContributors { get; init; } = [];

        public static DonorOrganization Create(string name, string? contactEmail, string? website)
        {
            return new DonorOrganization
            {
                Id = ObjectId.GenerateNewId(),
                Name = name,
                ContactEmail = contactEmail,
                Website = website,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        /// <summary>
        /// The hours logged on the organization's own donations, whatever their status.
        /// </summary>
        public int HoursSpent(IEnumerable<Donation> donations)
        {
            return OwnDonations(donations).Sum(d => d.HoursLogged);
        }

        /// <summary>
        /// The hours still to be logged on the organization's own Active donations.
        /// </summary>
        public int HoursAvailable(IEnumerable<Donation> donations)
        {
            return OwnDonations(donations)
                .Where(d => d.Status == DonationStatus.Active)
                .Sum(d => d.HoursCommitted - d.HoursLogged);
        }

        private IEnumerable<Donation> OwnDonations(IEnumerable<Donation> donations)
        {
            return donations.Where(d => d.DonorOrganizationId == Id);
        }
    }
}
using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;

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
        public List<Donation> Donations { get; set; } = [];

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
    }
}
using MongoDB.Bson;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.TestBuilder
{
    internal static class DonorOrganizationBuilder
    {
        internal static DonorOrganization BuildExisting(string? id = null)
        {
            var objectId = id != null ? new ObjectId(id) : new ObjectId(Constants.TestDonorOrganizationId);
            return new DonorOrganization
            {
                Id = objectId,
                Name = "Acme Corp",
                ContactEmail = "contact@acme.com",
                Website = "https://acme.com",
                CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
            };
        }
    }
}
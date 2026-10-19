using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal static class DonorOrganizationMapper
    {
        internal static DonorOrganizationDto ToDto(this DonorOrganization organization, IEnumerable<Domain.Donation> donations)
        {
            var own = donations.Where(d => d.DonorOrganizationId == organization.Id).ToList();

            return new DonorOrganizationDto
            {
                Id = organization.Id.ToString(),
                Name = organization.Name,
                ContactEmail = organization.ContactEmail,
                Website = organization.Website,
                CreatedAt = organization.CreatedAt,
                UpdatedAt = organization.UpdatedAt,
                HoursSpent = own.Sum(d => d.HoursLogged),
                HoursAvailable = own
                    .Where(d => d.Status == DonationStatus.Active)
                    .Sum(d => d.HoursCommitted - d.HoursLogged)
            };
        }

        internal static async Task<IReadOnlyList<Domain.Donation>> GetDonationsAsync(
            this IDonationRepository repository,
            IEnumerable<DonorOrganization> organizations,
            CancellationToken cancellationToken)
        {
            var ids = organizations.Select(o => o.Id.ToString()).ToList();
            return ids.Count == 0 ? [] : await repository.GetByDonorOrganizationIdsAsync(ids, cancellationToken);
        }
    }
}
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IDonorOrganizationRepository
    {
        Task<DonorOrganization?> GetByIdAsync(string id);
        Task<(IReadOnlyList<DonorOrganization> Organizations, int TotalCount)> GetAllAsync(int pageNumber, int pageSize);
        Task CreateAsync(DonorOrganization organization);
        Task UpdateAsync(DonorOrganization organization);
        Task DeleteAsync(string id);
        Task<DonorOrganization?> GetByNameAsync(string name);
    }
}
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Domain.Entities;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(string id);
        Task<(IReadOnlyList<Project> Projects, int TotalCount)> GetAllActiveAsync(int pageNumber, int pageSize);
        Task CreateAsync(Project project);
        Task UpdateAsync(Project project);
        Task<Project?> GetByGithubUrlAsync(Uri githubRepositoryUrl);
    }
}
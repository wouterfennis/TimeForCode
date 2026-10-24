using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Domain.Entities;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(string id);
        Task<(IReadOnlyList<Project> Projects, int TotalCount)> SearchActiveAsync(ProjectSearchCriteria criteria, CancellationToken cancellationToken);
        Task CreateAsync(Project project);
        Task UpdateAsync(Project project);
        Task<Project?> GetByGithubUrlAsync(Uri githubRepositoryUrl);

        /// <summary>
        /// Returns the projects published by the user in any lifecycle state, oldest first.
        /// </summary>
        Task<IReadOnlyList<Project>> GetByPublishedByUserIdAsync(string userId, CancellationToken cancellationToken);
    }
}
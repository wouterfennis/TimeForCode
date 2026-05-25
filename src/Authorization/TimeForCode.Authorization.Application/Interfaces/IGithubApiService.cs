using TimeForCode.Authorization.Application.Models;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IGithubApiService
    {
        Task<Result<IEnumerable<RepositoryInfo>>> GetUserRepositoriesAsync(string githubAccessToken);
        Task<Result<RepositoryInfo>> GetRepositoryAsync(string owner, string repo, string githubAccessToken);
    }
}
using TimeForCode.Authorization.Application.Models;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Interfaces
{
    /// <summary>
    /// Reads public GitHub data using the application's own credentials; no user token is involved.
    /// </summary>
    public interface IGithubApiService
    {
        Task<Result<IEnumerable<RepositoryInfo>>> GetUserRepositoriesAsync(string login);
        Task<Result<RepositoryInfo>> GetRepositoryAsync(string owner, string repo);
    }
}
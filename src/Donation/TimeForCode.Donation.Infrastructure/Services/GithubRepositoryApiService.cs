using Microsoft.Extensions.Options;
using RestSharp;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Infrastructure.Options;

namespace TimeForCode.Donation.Infrastructure.Services
{
    internal class GithubRepositoryApiService : IGithubRepositoryApiService
    {
        private readonly RestClient _restClient;
        private readonly GithubApiOptions _githubApiOptions;

        public GithubRepositoryApiService(RestClient restClient, IOptions<GithubApiOptions> githubApiOptions)
        {
            _restClient = restClient;
            _githubApiOptions = githubApiOptions.Value;
        }

        public async Task<Result<GithubSnapshot>> GetRepositoryMetadataAsync(Uri repositoryUrl)
        {
            if (!repositoryUrl.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return Result<GithubSnapshot>.Failure("URL must be a GitHub repository URL (github.com).");
            }

            var segments = repositoryUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2)
            {
                return Result<GithubSnapshot>.Failure("URL must point to a GitHub repository with exactly an owner and repository name.");
            }

            var repoPath = string.Join("/", segments);
            var request = new RestRequest($"{_githubApiOptions.BaseUrl}/repos/{repoPath}", Method.Get);
            request.AddHeader("User-Agent", "TimeForCode");
            request.AddHeader("Accept", "application/vnd.github+json");

            var response = await _restClient.ExecuteAsync<GithubRepositoryResponse>(request);

            if (!response.IsSuccessful || response.Data == null)
            {
                return Result<GithubSnapshot>.Failure("Failed to retrieve repository information from GitHub.");
            }

            var data = response.Data;
            var snapshot = new GithubSnapshot
            {
                Name = data.Name,
                FullName = data.FullName,
                Description = data.Description,
                HtmlUrl = data.HtmlUrl,
                Language = data.Language,
                Topics = data.Topics,
                StargazersCount = data.StargazersCount,
                ForksCount = data.ForksCount,
                OpenIssuesCount = data.OpenIssuesCount,
                Homepage = data.Homepage,
                DefaultBranch = data.DefaultBranch,
                License = data.License?.SpdxId,
                OwnerLogin = data.Owner.Login,
                OwnerAvatarUrl = data.Owner.AvatarUrl,
                CreatedAt = data.CreatedAt,
                UpdatedAt = data.UpdatedAt,
                PushedAt = data.PushedAt,
                IsPrivate = data.IsPrivate,
                IsArchived = data.Archived
            };

            return Result<GithubSnapshot>.Success(snapshot);
        }
    }
}
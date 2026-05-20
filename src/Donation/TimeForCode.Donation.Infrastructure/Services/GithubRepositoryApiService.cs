using RestSharp;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Services
{
    internal class GithubRepositoryApiService : IGithubRepositoryApiService
    {
        private const string GithubApiBase = "https://api.github.com";
        private readonly RestClient _restClient;

        public GithubRepositoryApiService(RestClient restClient)
        {
            _restClient = restClient;
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
            var request = new RestRequest($"{GithubApiBase}/repos/{repoPath}", Method.Get);
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

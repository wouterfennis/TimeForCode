using MediatR;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class RegisterProjectHandler : IRequestHandler<RegisterProjectCommand, Result<RegisterProjectResult>>
    {
        private readonly IGithubRepositoryApiService _githubService;
        private readonly IProjectRepository _projectRepository;

        public RegisterProjectHandler(
            IGithubRepositoryApiService githubService,
            IProjectRepository projectRepository)
        {
            _githubService = githubService;
            _projectRepository = projectRepository;
        }

        public async Task<Result<RegisterProjectResult>> Handle(RegisterProjectCommand request, CancellationToken cancellationToken)
        {
            if (!string.Equals(request.GithubRepositoryUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RegisterProjectResult>.Failure("Repository URL must use HTTPS.");
            }

            if (!string.Equals(request.GithubRepositoryUrl.Host, "github.com", StringComparison.OrdinalIgnoreCase))
            {
                return Result<RegisterProjectResult>.Failure("Repository URL must point to github.com.");
            }

            var segments = request.GithubRepositoryUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return Result<RegisterProjectResult>.Failure("Invalid GitHub repository URL: must include owner and repository name.");
            }

            if (segments.Any(s => s == ".."))
            {
                return Result<RegisterProjectResult>.Failure("Invalid GitHub repository URL: path traversal is not allowed.");
            }

            if (!IsValidGithubSegment(segments[0]) || !IsValidGithubSegment(segments[1]))
            {
                return Result<RegisterProjectResult>.Failure("Invalid GitHub repository URL: owner or repository name contains invalid characters.");
            }

            var metadataResult = await _githubService.GetRepositoryMetadataAsync(request.GithubRepositoryUrl);
            if (metadataResult.IsFailure)
            {
                return Result<RegisterProjectResult>.Failure(metadataResult.ErrorMessage);
            }

            var snapshot = metadataResult.Value;

            if (snapshot.IsPrivate || snapshot.IsArchived)
            {
                return Result<RegisterProjectResult>.Failure("Repository must be public and not archived.");
            }

            var normalizedUrl = new Uri($"{request.GithubRepositoryUrl.Scheme}://{request.GithubRepositoryUrl.Host}/{segments[0]}/{segments[1]}");

            var existing = await _projectRepository.GetByGithubUrlAsync(normalizedUrl);
            if (existing != null && existing.Status == ProjectStatus.Published)
            {
                return Result<RegisterProjectResult>.Conflict("Repository is already published.");
            }

            if (existing != null && existing.Status == ProjectStatus.Archived)
            {
                existing.Status = ProjectStatus.Published;
                existing.Snapshot = snapshot;
                existing.PublishedAt = DateTimeOffset.UtcNow;
                await _projectRepository.UpdateAsync(existing);

                return Result<RegisterProjectResult>.Success(new RegisterProjectResult
                {
                    ProjectId = existing.Id.ToString()
                });
            }

            var project = Project.Create(
                snapshot,
                normalizedUrl,
                request.UserId,
                DateTimeOffset.UtcNow);

            try
            {
                await _projectRepository.CreateAsync(project);
            }
            catch (RepositoryConflictException)
            {
                return Result<RegisterProjectResult>.Conflict("Repository is already published.");
            }

            return Result<RegisterProjectResult>.Success(new RegisterProjectResult
            {
                ProjectId = project.Id.ToString()
            });
        }

        // GitHub allows alphanumeric characters, hyphens, underscores, and dots in owner/repo names.
        private static bool IsValidGithubSegment(string segment)
        {
            return !string.IsNullOrEmpty(segment) &&
                   segment.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.');
        }
    }
}
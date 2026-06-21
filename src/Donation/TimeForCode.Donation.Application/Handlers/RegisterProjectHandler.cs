using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<RegisterProjectHandler> _logger;

        public RegisterProjectHandler(
            IGithubRepositoryApiService githubService,
            IProjectRepository projectRepository,
            ILogger<RegisterProjectHandler> logger)
        {
            _githubService = githubService;
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<Result<RegisterProjectResult>> Handle(RegisterProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registering project from URL {GithubRepositoryUrl} for user {UserId}", request.GithubRepositoryUrl, request.UserId);

            var metadataResult = await _githubService.GetRepositoryMetadataAsync(request.GithubRepositoryUrl);
            if (metadataResult.IsFailure)
            {
                _logger.LogWarning("Failed to get repository metadata for {GithubRepositoryUrl}: {Error}", request.GithubRepositoryUrl, metadataResult.ErrorMessage);
                return Result<RegisterProjectResult>.Failure(metadataResult.ErrorMessage);
            }

            var snapshot = metadataResult.Value;

            if (snapshot.IsPrivate || snapshot.IsArchived)
            {
                _logger.LogWarning("Repository {GithubRepositoryUrl} is private or archived", request.GithubRepositoryUrl);
                return Result<RegisterProjectResult>.Failure("Repository must be public and not archived.");
            }

            var segments = request.GithubRepositoryUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                _logger.LogWarning("Invalid GitHub repository URL: {GithubRepositoryUrl}", request.GithubRepositoryUrl);
                return Result<RegisterProjectResult>.Failure("Invalid GitHub repository URL.");
            }

            var normalizedUrl = new Uri($"{request.GithubRepositoryUrl.Scheme}://{request.GithubRepositoryUrl.Host}/{segments[0]}/{segments[1]}");

            var existing = await _projectRepository.GetByGithubUrlAsync(normalizedUrl);
            if (existing != null && existing.Status == ProjectStatus.Published)
            {
                _logger.LogWarning("Repository {NormalizedUrl} is already published", normalizedUrl);
                return Result<RegisterProjectResult>.Conflict("Repository is already published.");
            }

            if (existing != null && existing.Status == ProjectStatus.Archived)
            {
                _logger.LogInformation("Re-publishing archived project {ProjectId} for URL {NormalizedUrl}", existing.Id, normalizedUrl);
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
            catch (RepositoryConflictException exception)
            {
                _logger.LogWarning(exception, "Conflict while creating project for URL {NormalizedUrl}", normalizedUrl);
                return Result<RegisterProjectResult>.Conflict("Repository is already published.");
            }

            _logger.LogInformation("Project {ProjectId} registered successfully for URL {NormalizedUrl}", project.Id, normalizedUrl);

            return Result<RegisterProjectResult>.Success(new RegisterProjectResult
            {
                ProjectId = project.Id.ToString()
            });
        }
    }
}
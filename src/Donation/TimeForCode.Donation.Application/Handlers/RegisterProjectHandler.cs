using MediatR;
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
            var metadataResult = await _githubService.GetRepositoryMetadataAsync(request.GithubRepositoryUrl);
            if (metadataResult.IsFailure)
            {
                return Result<RegisterProjectResult>.Failure("Failed to retrieve repository information from GitHub.");
            }

            var snapshot = metadataResult.Value;

            if (snapshot.IsPrivate || snapshot.IsArchived)
            {
                return Result<RegisterProjectResult>.Failure("Repository must be public and not archived.");
            }

            var existing = await _projectRepository.GetByGithubUrlAsync(request.GithubRepositoryUrl);
            if (existing != null)
            {
                return Result<RegisterProjectResult>.Conflict("Repository is already published.");
            }

            var project = Project.Create(
                snapshot,
                request.GithubRepositoryUrl,
                request.UserId,
                DateTimeOffset.UtcNow);

            await _projectRepository.CreateAsync(project);

            return Result<RegisterProjectResult>.Success(new RegisterProjectResult
            {
                ProjectId = project.Id.ToString()
            });
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetMaintainedProjectHandler : IRequestHandler<GetMaintainedProjectQuery, Result<GetMaintainedProjectResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<GetMaintainedProjectHandler> _logger;

        public GetMaintainedProjectHandler(IProjectRepository projectRepository, ILogger<GetMaintainedProjectHandler> logger)
        {
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<Result<GetMaintainedProjectResult>> Handle(GetMaintainedProjectQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Project {ProjectId}: maintainer read requested by user {UserId}", request.ProjectId, request.UserId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<GetMaintainedProjectResult>.Failure("Project not found.");
            }

            if (project.PublishedByUserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} is not the maintainer of project {ProjectId}", request.UserId, request.ProjectId);
                return Result<GetMaintainedProjectResult>.Forbidden("Only the maintainer of the project can do this.");
            }

            var dto = new ProjectDto
            {
                Id = project.Id.ToString(),
                Name = project.Snapshot.Name,
                FullName = project.Snapshot.FullName,
                Description = project.Snapshot.Description,
                GithubUrl = project.Snapshot.HtmlUrl,
                Language = project.Snapshot.Language,
                Topics = project.Snapshot.Topics,
                StargazersCount = project.Snapshot.StargazersCount,
                ForksCount = project.Snapshot.ForksCount,
                OpenIssuesCount = project.Snapshot.OpenIssuesCount,
                Homepage = project.Snapshot.Homepage,
                DefaultBranch = project.Snapshot.DefaultBranch,
                License = project.Snapshot.License,
                OwnerLogin = project.Snapshot.OwnerLogin,
                OwnerAvatarUrl = project.Snapshot.OwnerAvatarUrl,
                CreatedAt = project.Snapshot.CreatedAt,
                UpdatedAt = project.Snapshot.UpdatedAt,
                PushedAt = project.Snapshot.PushedAt,
                Status = project.Status
            };

            return Result<GetMaintainedProjectResult>.Success(new GetMaintainedProjectResult
            {
                Project = dto,
                ReviewerReason = project.ChangeRequestReason
            });
        }
    }
}
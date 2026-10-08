using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class ArchiveProjectHandler : IRequestHandler<ArchiveProjectCommand, Result<ProjectLifecycleResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<ArchiveProjectHandler> _logger;

        public ArchiveProjectHandler(IProjectRepository projectRepository, ILogger<ArchiveProjectHandler> logger)
        {
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<Result<ProjectLifecycleResult>> Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Project {ProjectId}: archive requested by user {UserId}", request.ProjectId, request.UserId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<ProjectLifecycleResult>.Failure("Project not found.");
            }

            if (project.PublishedByUserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} is not the maintainer of project {ProjectId}", request.UserId, request.ProjectId);
                return Result<ProjectLifecycleResult>.Forbidden("Only the maintainer of the project can do this.");
            }

            var transition = project.Archive();
            if (!transition.IsSuccess)
            {
                _logger.LogWarning("Project {ProjectId}: archive rejected: {Error}", request.ProjectId, transition.ErrorMessage);
                return Result<ProjectLifecycleResult>.Conflict(transition.ErrorMessage);
            }

            await _projectRepository.UpdateAsync(project);

            return Result<ProjectLifecycleResult>.Success(new ProjectLifecycleResult
            {
                ProjectId = project.Id.ToString(),
                Status = project.Status,
                ReviewerReason = project.ChangeRequestReason
            });
        }
    }
}